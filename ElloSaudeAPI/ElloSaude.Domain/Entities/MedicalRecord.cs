namespace ElloSaude.Domain.Entities;

public class MedicalRecord : BaseEntity
{
    public Guid? AppointmentId { get; private set; }
    public bool IsLegacyUnlinked { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public string Description { get; private set; }
    public string Diagnosis { get; private set; }
    public string? TreatmentPlan { get; private set; }
    public bool IsSigned { get; private set; } = false;
    public DateTime? SignedAt { get; private set; }
    public ICollection<MedicalRecordAddendum> Addenda { get; private set; }

    private MedicalRecord()
    {
        Description = string.Empty;
        Diagnosis = string.Empty;
        Addenda = new List<MedicalRecordAddendum>();
    }

    public MedicalRecord(Guid patientId, Guid doctorId, string description, string diagnosis,
        string tenantId, Guid appointmentId, string? treatmentPlan = null)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        Description = description;
        Diagnosis = diagnosis;
        TenantId = tenantId;
        AppointmentId = appointmentId;
        TreatmentPlan = treatmentPlan;
        CreatedAt = DateTime.UtcNow;
        Addenda = new List<MedicalRecordAddendum>();
    }

    public void Update(string description, string diagnosis, string? treatmentPlan = null)
    {
        if (IsSigned)
        {
            throw new InvalidOperationException("Prontuário já assinado e finalizado. Não é permitida a alteração direta; adicione uma evolução/adendo.");
        }

        Description = description;
        Diagnosis = diagnosis;
        TreatmentPlan = treatmentPlan;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Sign(Guid doctorId)
    {
        if (DoctorId != doctorId)
        {
            throw new UnauthorizedAccessException("Apenas o médico responsável pode assinar este prontuário.");
        }

        IsSigned = true;
        SignedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddAddendum(Guid doctorId, string note, string tenantId)
    {
        if (DoctorId != doctorId)
        {
            throw new UnauthorizedAccessException("Apenas o profissional responsável pode adicionar uma evolução.");
        }

        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("A nota do adendo não pode estar vazia.", nameof(note));
        }

        Addenda.Add(new MedicalRecordAddendum(Id, doctorId, note, tenantId));
        UpdatedAt = DateTime.UtcNow;
    }
}