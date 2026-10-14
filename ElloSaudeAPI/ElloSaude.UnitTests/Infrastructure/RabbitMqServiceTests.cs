using ElloSaude.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ElloSaude.UnitTests.Infrastructure;

public class RabbitMqServiceTests
{
    [Fact]
    public void Constructor_WithInvalidPort_ThrowsConfigurationError()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:Port"] = "not-a-port"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(
            () => new RabbitMqService(configuration));

        Assert.Contains("RabbitMq:Port", exception.Message);
    }

    [Fact]
    public async Task PublishAsync_WithBlankQueue_ThrowsWithoutConnecting()
    {
        var configuration = new ConfigurationBuilder().Build();
        await using var service = new RabbitMqService(configuration);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.PublishAsync(" ", new { Id = 1 }));
    }
}
