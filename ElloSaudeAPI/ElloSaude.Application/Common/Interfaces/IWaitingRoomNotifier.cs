namespace ElloSaude.Application.Common.Interfaces;

public interface IWaitingRoomNotifier
{
    Task NotifyPatientArrivedAsync(
        Guid professionalUserId,
        Guid appointmentId,
        DateTimeOffset arrivedAt,
        CancellationToken cancellationToken);
}
