namespace ElloSaude.Domain.Entities;

public class MedicalRecordAddendum : BaseEntity
{
    public Guid MedicalRecordId { get; private set; }
    public Guid DoctorId { get; private set; }
    public string Note { get; private set; }

    private MedicalRecordAddendum()
    {
        Note = string.Empty;
    }

    public MedicalRecordAddendum(Guid medicalRecordId, Guid doctorId, string note, string tenantId)
    {
        MedicalRecordId = medicalRecordId;
        DoctorId = doctorId;
        Note = note;
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
    }
}
