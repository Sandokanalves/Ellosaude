using ElloSaude.Domain.Enums;

namespace ElloSaude.Domain.Entities;

public class Appointment : BaseEntity
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid ProfessionalId { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public AppointmentType Type { get; private set; }
    public decimal Price { get; private set; }
    public bool IsFreeReturn { get; private set; }
    public string? Observations { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTime? CancellationDate { get; private set; }

    private Appointment() { TenantId = null!; }

    public Appointment(DateTime start, DateTime end, Guid patientId, Guid professionalId,
        AppointmentType type, string tenantId, decimal price = 0, bool isFreeReturn = false, string? observations = null)
    {
        StartTime = start;
        EndTime = end;
        PatientId = patientId;
        ProfessionalId = professionalId;
        Type = type;
        Status = AppointmentStatus.Pendente;
        TenantId = tenantId;
        Price = isFreeReturn ? 0 : price;
        IsFreeReturn = isFreeReturn;
        Observations = observations;
    }

    public void Confirm()
    {
        Status = AppointmentStatus.Confirmado;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = AppointmentStatus.Realizado;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFreeReturn()
    {
        if (Type != AppointmentType.Retorno)
        {
            throw new InvalidOperationException("Somente um agendamento de retorno pode ser gratuito.");
        }

        if (Status == AppointmentStatus.Cancelado)
        {
            throw new InvalidOperationException("Um agendamento cancelado não pode ser marcado como retorno gratuito.");
        }

        IsFreeReturn = true;
        Price = 0;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        Status = AppointmentStatus.Cancelado;
        CancellationReason = reason;
        CancellationDate = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reschedule(DateTime newStart, DateTime newEnd)
    {
        StartTime = newStart;
        EndTime = newEnd;
        Status = AppointmentStatus.Pendente;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(AppointmentStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}