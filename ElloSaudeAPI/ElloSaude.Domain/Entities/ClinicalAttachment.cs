namespace ElloSaude.Domain.Entities;

public class ClinicalAttachment : BaseEntity
{
    public Guid PatientId { get; private set; }
    public Guid ProfessionalId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public string Title { get; private set; }
    public string Category { get; private set; } // Exame, FotoAntesDepois, Laudo, Outros
    public string ContentType { get; private set; }
    public string FileUrl { get; private set; }
    public string? Notes { get; private set; }

    private ClinicalAttachment()
    {
        Title = string.Empty;
        Category = "Exame";
        ContentType = "application/pdf";
        FileUrl = string.Empty;
    }

    public ClinicalAttachment(Guid patientId, Guid professionalId, string title, string category,
        string contentType, string fileUrl, string tenantId, Guid? appointmentId = null, string? notes = null)
    {
        PatientId = patientId;
        ProfessionalId = professionalId;
        Title = title;
        Category = category;
        ContentType = contentType;
        FileUrl = fileUrl;
        TenantId = tenantId;
        AppointmentId = appointmentId;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }
}
