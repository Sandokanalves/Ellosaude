namespace ElloSaude.Application.Common.Interfaces;

public interface IMessageBusService
{
    Task PublishAsync(string queue, object message, CancellationToken cancellationToken = default);
}
