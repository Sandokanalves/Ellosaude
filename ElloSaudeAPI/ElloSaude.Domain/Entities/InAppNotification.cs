namespace ElloSaude.Domain.Entities;

public sealed class InAppNotification : BaseEntity
{
    private InAppNotification() { TenantId = null!; }

    public InAppNotification(
        Guid eventId,
        Guid recipientUserId,
        Guid appointmentId,
        DateTime occurredAt,
        string tenantId)
    {
        EventId = eventId;
        RecipientUserId = recipientUserId;
        AppointmentId = appointmentId;
        Type = "PatientArrived";
        Title = "Paciente chegou";
        Message = "Um paciente está aguardando na recepção.";
        OccurredAt = occurredAt;
        TenantId = tenantId;
    }

    public Guid EventId { get; private set; }
    public Guid RecipientUserId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string Type { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public DateTime OccurredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? RealtimeDeliveredAt { get; private set; }

    public void MarkRead(DateTime readAt) => ReadAt ??= readAt;

    public void MarkRealtimeDelivered(DateTime deliveredAt) =>
        RealtimeDeliveredAt ??= deliveredAt;
}
