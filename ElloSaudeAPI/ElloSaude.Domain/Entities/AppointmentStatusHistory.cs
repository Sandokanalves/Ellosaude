using ElloSaude.Domain.Enums;

namespace ElloSaude.Domain.Entities;

public sealed class AppointmentStatusHistory : BaseEntity
{
    public Guid AppointmentId { get; private set; }
    public AppointmentStatus PreviousStatus { get; private set; }
    public AppointmentStatus NewStatus { get; private set; }
    public Guid? ChangedByUserId { get; private set; }
    public string ChangedByRole { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private AppointmentStatusHistory()
    {
        ChangedByRole = string.Empty;
    }

    public AppointmentStatusHistory(
        Guid appointmentId,
        AppointmentStatus previousStatus,
        AppointmentStatus newStatus,
        Guid? changedByUserId,
        string changedByRole,
        DateTime occurredAt,
        string tenantId)
    {
        if (appointmentId == Guid.Empty)
            throw new ArgumentException("O agendamento é obrigatório.", nameof(appointmentId));
        if (!Enum.IsDefined(previousStatus))
            throw new ArgumentOutOfRangeException(nameof(previousStatus));
        if (!Enum.IsDefined(newStatus))
            throw new ArgumentOutOfRangeException(nameof(newStatus));
        if (previousStatus == newStatus)
            throw new ArgumentException("O status anterior e o novo status devem ser diferentes.");
        if (string.IsNullOrWhiteSpace(changedByRole))
            throw new ArgumentException("O papel do usuário é obrigatório.", nameof(changedByRole));
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("A clínica é obrigatória.", nameof(tenantId));

        AppointmentId = appointmentId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedByUserId = changedByUserId == Guid.Empty ? null : changedByUserId;
        ChangedByRole = changedByRole;
        OccurredAt = DateTime.SpecifyKind(occurredAt, DateTimeKind.Utc);
        TenantId = tenantId;
        CreatedAt = OccurredAt;
    }
}
