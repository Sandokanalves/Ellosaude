using System.Globalization;
using System.Text;
using System.Text.Json;
using ElloSaude.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ElloSaude.Infrastructure.Messaging;

public sealed class WaitingRoomNotificationConsumerService : BackgroundService
{
    private const string DeadLetterExchange = "ellosaude.waiting-room.dlx";
    private const string DeadLetterQueue = "ellosaude.waiting-room.dead";
    private const string RetryQueue = "ellosaude.waiting-room.retry";
    private const string RetryHeader = "x-ellosaude-retry-count";
    private const int MaximumRetries = 5;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqConnectionProvider _connectionProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaitingRoomNotificationConsumerService> _logger;

    public WaitingRoomNotificationConsumerService(
        RabbitMqConnectionProvider connectionProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<WaitingRoomNotificationConsumerService> logger)
    {
        _connectionProvider = connectionProvider;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Consumidor da sala de espera desconectado do RabbitMQ.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _connectionProvider.ConnectAsync(cancellationToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        await using var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);

        await DeclareTopologyAsync(channel, cancellationToken);
        await channel.BasicQosAsync(0, 1, false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, delivery) =>
            await ProcessDeliveryAsync(channel, delivery, cancellationToken);

        await channel.BasicConsumeAsync(
            queue: WaitingRoomQueues.PatientArrived,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    private static async Task DeclareTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: DeadLetterQueue,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: WaitingRoomQueues.PatientArrived,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange,
                ["x-dead-letter-routing-key"] = DeadLetterQueue
            },
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: RetryQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-message-ttl"] = (int)RetryDelay.TotalMilliseconds,
                ["x-dead-letter-exchange"] = string.Empty,
                ["x-dead-letter-routing-key"] = WaitingRoomQueues.PatientArrived
            },
            cancellationToken: cancellationToken);
    }

    private async Task ProcessDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        try
        {
            var notificationEvent = JsonSerializer.Deserialize<WaitingRoomPatientArrivedEvent>(
                delivery.Body.Span)
                ?? throw new JsonException("Mensagem sem payload de evento.");

            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<WaitingRoomPatientArrivedHandler>();
            await handler.HandleAsync(notificationEvent, cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await channel.BasicNackAsync(
                delivery.DeliveryTag,
                multiple: false,
                requeue: true,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            int retryCount;
            try
            {
                retryCount = ReadRetryCount(delivery.BasicProperties.Headers);
            }
            catch (InvalidOperationException headerException)
            {
                _logger.LogError(headerException, "Mensagem RabbitMQ com contador de tentativas inválido.");
                await channel.BasicNackAsync(
                    delivery.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken);
                return;
            }

            _logger.LogError(
                exception,
                "Falha ao processar notificação da sala de espera; tentativa {RetryCount} de {MaximumRetries}.",
                retryCount + 1,
                MaximumRetries);

            if (retryCount >= MaximumRetries)
            {
                await channel.BasicNackAsync(
                    delivery.DeliveryTag,
                    multiple: false,
                    requeue: false,
                    cancellationToken);
                _logger.LogCritical(
                    "Mensagem de sala de espera enviada à fila de mensagens mortas após {RetryCount} tentativas.",
                    retryCount);
                return;
            }

            try
            {
                await RepublishForRetryAsync(channel, delivery, retryCount + 1, cancellationToken);
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
            }
            catch (Exception publishException)
            {
                _logger.LogError(publishException, "Falha ao agendar nova tentativa da mensagem.");
                await channel.BasicNackAsync(
                    delivery.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken);
            }
        }
    }

    private static int ReadRetryCount(IDictionary<string, object?>? headers)
    {
        if (headers is null || !headers.TryGetValue(RetryHeader, out var rawValue))
            return 0;

        try
        {
            var retryCount = rawValue switch
            {
                byte value => value,
                short value => value,
                int value => value,
                long value => checked((int)value),
                byte[] value => int.Parse(Encoding.ASCII.GetString(value), CultureInfo.InvariantCulture),
                _ => Convert.ToInt32(rawValue, CultureInfo.InvariantCulture)
            };
            return retryCount >= 0
                ? retryCount
                : throw new InvalidOperationException("O contador de tentativas não pode ser negativo.");
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or InvalidCastException)
        {
            throw new InvalidOperationException("O contador de tentativas RabbitMQ é inválido.", exception);
        }
    }

    private static async Task RepublishForRetryAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        int retryCount,
        CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            ContentType = delivery.BasicProperties.ContentType ?? "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = delivery.BasicProperties.MessageId ?? Guid.NewGuid().ToString(),
            CorrelationId = delivery.BasicProperties.CorrelationId,
            Headers = new Dictionary<string, object?>
            {
                [RetryHeader] = retryCount
            }
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: RetryQueue,
            mandatory: true,
            basicProperties: properties,
            body: delivery.Body,
            cancellationToken: cancellationToken);
    }
}
