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
    public DateTime? ArrivedAt { get; private set; }
    public DateTime? ServiceStartedAt { get; private set; }

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
        if (Status != AppointmentStatus.Pendente)
            throw new InvalidOperationException("Somente um agendamento pendente pode ser confirmado.");
        Status = AppointmentStatus.Confirmado;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(DateTime? occurredAt = null)
    {
        if (Status != AppointmentStatus.EmAtendimento)
            throw new InvalidOperationException("Somente um atendimento em andamento pode ser finalizado.");
        Status = AppointmentStatus.Realizado;
        UpdatedAt = occurredAt ?? DateTime.UtcNow;
    }

    public void MarkAsWaiting(DateTime? arrivedAt = null)
    {
        if (Status != AppointmentStatus.Confirmado)
            throw new InvalidOperationException("Somente um agendamento confirmado pode aguardar na recepção.");

        ArrivedAt = arrivedAt ?? DateTime.UtcNow;
        Status = AppointmentStatus.AguardandoRecepcao;
        UpdatedAt = ArrivedAt;
    }

    public void StartService(DateTime? startedAt = null)
    {
        if (Status != AppointmentStatus.AguardandoRecepcao)
            throw new InvalidOperationException("Somente um paciente aguardando na recepção pode iniciar o atendimento.");

        ServiceStartedAt = startedAt ?? DateTime.UtcNow;
        Status = AppointmentStatus.EmAtendimento;
        UpdatedAt = ServiceStartedAt;
    }

    public void MarkAsNoShow(DateTime? occurredAt = null)
    {
        var markedAt = occurredAt ?? DateTime.UtcNow;
        if (Status != AppointmentStatus.Confirmado)
            throw new InvalidOperationException("Somente um agendamento confirmado sem chegada pode ser marcado como falta.");
        if (markedAt < StartTime)
            throw new InvalidOperationException("O agendamento só pode ser marcado como falta após o horário previsto.");

        Status = AppointmentStatus.Faltou;
        UpdatedAt = markedAt;
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
        if (Status is not (AppointmentStatus.Pendente or AppointmentStatus.Confirmado))
            throw new InvalidOperationException("Somente um agendamento pendente ou confirmado pode ser cancelado.");
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
        ArrivedAt = null;
        ServiceStartedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(AppointmentStatus status, DateTime? occurredAt = null)
    {
        if (status == Status)
            return;

        switch (status)
        {
            case AppointmentStatus.Confirmado:
                Confirm();
                break;
            case AppointmentStatus.AguardandoRecepcao:
                MarkAsWaiting(occurredAt);
                break;
            case AppointmentStatus.EmAtendimento:
                StartService(occurredAt);
                break;
            case AppointmentStatus.Realizado:
                Complete(occurredAt);
                break;
            case AppointmentStatus.Faltou:
                MarkAsNoShow(occurredAt);
                break;
            case AppointmentStatus.Cancelado:
                Cancel("Cancelado pela equipe.");
                break;
            default:
                throw new InvalidOperationException("A transição de status solicitada não é permitida.");
        }
    }
}