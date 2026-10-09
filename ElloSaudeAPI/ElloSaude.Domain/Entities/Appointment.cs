// ElloSaude.Domain/Entities/Appointment.cs
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;

namespace ElloSaude.Domain.Entities;

public class Appointment : BaseEntity
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public Guid PatientId { get; private set; }

    // ADICIONE ESTA LINHA:
    public Guid ProfessionalId { get; private set; }

    public AppointmentStatus Status { get; private set; }
    public AppointmentType Type { get; private set; }

    private Appointment() { TenantId = null!; }

    public Appointment(DateTime start, DateTime end, Guid patientId, Guid professionalId, AppointmentType type, string tenantId)
    {
        StartTime = start;
        EndTime = end;
        PatientId = patientId;
        ProfessionalId = professionalId; // Atribua aqui
        Type = type;
        Status = AppointmentStatus.Pendente;
        TenantId = tenantId;
    }
}