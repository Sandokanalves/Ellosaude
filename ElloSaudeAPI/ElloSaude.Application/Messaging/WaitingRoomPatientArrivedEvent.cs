namespace ElloSaude.Application.Messaging;

public static class WaitingRoomQueues
{
    public const string PatientArrived = "ellosaude.waiting-room.patient-arrived";
}

public sealed record WaitingRoomPatientArrivedEvent(
    Guid EventId,
    Guid AppointmentId,
    Guid ProfessionalUserId,
    DateTimeOffset ArrivedAt,
    string TenantId);
