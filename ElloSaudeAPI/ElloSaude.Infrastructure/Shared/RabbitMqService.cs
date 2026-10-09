using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using ElloSaude.Application.Common.Interfaces;

namespace ElloSaude.Infrastructure.Messaging;

public class RabbitMqService : IMessageBusService, IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly ConnectionFactory _factory;

    public RabbitMqService(IConfiguration configuration)
    {
        _factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMq:Host"] ?? "localhost",
            UserName = configuration["RabbitMq:UserName"] ?? "guest",
            Password = configuration["RabbitMq:Password"] ?? "guest",
            Port = int.Parse(configuration["RabbitMq:Port"] ?? "5672")
        };
    }

    private async Task EnsureConnectionAsync()
    {
        if (_connection == null || !_connection.IsOpen)
        {
            _connection = await _factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
        }
    }

    public void Publish(string queue, object message)
    {
        // Executa a tarefa assíncrona de forma segura para métodos síncronos
        Task.Run(async () =>
        {
            await EnsureConnectionAsync();

            await _channel!.QueueDeclareAsync(
                queue: queue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                body: body);
        }).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}