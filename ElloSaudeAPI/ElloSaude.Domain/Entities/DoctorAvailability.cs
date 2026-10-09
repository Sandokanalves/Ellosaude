namespace ElloSaude.Domain.Entities;

public class DoctorAvailability : BaseEntity
{
    public Guid ProfessionalId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public TimeSpan? BreakStartTime { get; private set; }
    public TimeSpan? BreakEndTime { get; private set; }
    public int SlotDurationMinutes { get; private set; } = 30;
    public bool IsActive { get; private set; } = true;

    private DoctorAvailability() { }

    public DoctorAvailability(Guid professionalId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime,
        string tenantId, TimeSpan? breakStartTime = null, TimeSpan? breakEndTime = null, int slotDurationMinutes = 30)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("O horário de término deve ser posterior ao início.");
        }

        ProfessionalId = professionalId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        TenantId = tenantId;
        BreakStartTime = breakStartTime;
        BreakEndTime = breakEndTime;
        SlotDurationMinutes = slotDurationMinutes > 0 ? slotDurationMinutes : 30;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(TimeSpan startTime, TimeSpan endTime, TimeSpan? breakStartTime, TimeSpan? breakEndTime, int slotDurationMinutes)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("O horário de término deve ser posterior ao início.");
        }

        StartTime = startTime;
        EndTime = endTime;
        BreakStartTime = breakStartTime;
        BreakEndTime = breakEndTime;
        SlotDurationMinutes = slotDurationMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
