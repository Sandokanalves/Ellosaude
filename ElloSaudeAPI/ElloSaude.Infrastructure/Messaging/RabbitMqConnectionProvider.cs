using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace ElloSaude.Infrastructure.Messaging;

public sealed class RabbitMqConnectionProvider
{
    private readonly ConnectionFactory _factory;

    public RabbitMqConnectionProvider(IConfiguration configuration)
    {
        var host = configuration["RabbitMq:Host"] ?? "localhost";
        var userName = configuration["RabbitMq:UserName"] ?? "guest";
        var password = configuration["RabbitMq:Password"] ?? "guest";
        var portValue = configuration["RabbitMq:Port"] ?? "5672";

        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("A configuração RabbitMq:Host é obrigatória.");
        if (string.IsNullOrWhiteSpace(userName))
            throw new InvalidOperationException("A configuração RabbitMq:UserName é obrigatória.");
        if (!int.TryParse(portValue, out var port) || port is < 1 or > 65535)
            throw new InvalidOperationException("A configuração RabbitMq:Port deve ser uma porta TCP válida.");

        _factory = new ConnectionFactory
        {
            HostName = host,
            UserName = userName,
            Password = password,
            Port = port,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(30)
        };
    }

    public Task<IConnection> ConnectAsync(CancellationToken cancellationToken) =>
        _factory.CreateConnectionAsync(cancellationToken);
}
