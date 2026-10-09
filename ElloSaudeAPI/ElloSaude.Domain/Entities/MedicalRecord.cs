namespace ElloSaude.Domain.Entities;

public class MedicalRecord : BaseEntity
{
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public string Description { get; private set; }
    public string Diagnosis { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public MedicalRecord(Guid patientId, Guid doctorId, string description, string diagnosis, string tenantId)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        Description = description;
        Diagnosis = diagnosis;
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string description, string diagnosis)
    {
        Description = description;
        Diagnosis = diagnosis;
    }
}