using Xunit;

namespace ElloSaude.IntegrationTests;

[AttributeUsage(AttributeTargets.Method)]
public sealed class RabbitMqFactAttribute : FactAttribute
{
    public RabbitMqFactAttribute()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable("RUN_RABBITMQ_TESTS"),
                "1",
                StringComparison.Ordinal))
        {
            Skip = "Defina RUN_RABBITMQ_TESTS=1 e inicie o serviço RabbitMQ local.";
        }
    }
}
