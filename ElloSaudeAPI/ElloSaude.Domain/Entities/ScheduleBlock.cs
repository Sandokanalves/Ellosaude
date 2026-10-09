namespace ElloSaude.Domain.Entities;

public class ScheduleBlock : BaseEntity
{
    public Guid ProfessionalId { get; private set; }
    public DateTime StartDateTime { get; private set; }
    public DateTime EndDateTime { get; private set; }
    public string Reason { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ScheduleBlock()
    {
        Reason = string.Empty;
    }

    public ScheduleBlock(Guid professionalId, DateTime startDateTime, DateTime endDateTime, string reason, string tenantId)
    {
        if (endDateTime <= startDateTime)
        {
            throw new ArgumentException("A data/hora de término deve ser posterior ao início.");
        }

        ProfessionalId = professionalId;
        StartDateTime = startDateTime;
        EndDateTime = endDateTime;
        Reason = reason;
        TenantId = tenantId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        IsActive = false;
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
