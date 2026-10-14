using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using ElloSaude.Application.Common.Interfaces;

namespace ElloSaude.Infrastructure.Messaging;

public sealed class RabbitMqService : IMessageBusService, IAsyncDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _publishLock = new(1, 1);
    private bool _disposed;

    public RabbitMqService(IConfiguration configuration)
    {
        var portValue = configuration["RabbitMq:Port"] ?? "5672";
        if (!int.TryParse(portValue, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("A configuração RabbitMq:Port deve ser uma porta TCP válida.");

        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"] ?? "localhost",
            UserName = configuration["RabbitMq:UserName"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            Port = port
        };
    }

    private async Task EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection?.IsOpen == true && _channel?.IsOpen == true)
            return;

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _connection = await _factory.CreateConnectionAsync(cancellationToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        _channel = await _connection.CreateChannelAsync(channelOptions, cancellationToken);
    }

    public async Task PublishAsync(
        string queue,
        object message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        ArgumentNullException.ThrowIfNull(message);
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _publishLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await EnsureConnectionAsync(cancellationToken);

            await _channel!.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = Guid.NewGuid().ToString()
            };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _publishLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _publishLock.WaitAsync();
        try
        {
            if (_disposed)
                return;

            _disposed = true;
            if (_channel is not null)
            {
                await _channel.DisposeAsync();
                _channel = null;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _publishLock.Release();
        }
    }
}