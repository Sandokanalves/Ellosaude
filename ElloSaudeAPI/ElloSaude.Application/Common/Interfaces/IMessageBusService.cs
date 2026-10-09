namespace ElloSaude.Application.Common.Interfaces;

public interface IMessageBusService
{
    void Publish(string queue, object message);
}
