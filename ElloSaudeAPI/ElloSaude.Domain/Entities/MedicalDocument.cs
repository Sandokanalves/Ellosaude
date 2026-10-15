namespace ElloSaude.Domain.Entities;

public class MedicalDocument : BaseEntity
{
    public Guid PatientId { get; private set; }
    public Guid ProfessionalId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public string DocumentType { get; private set; } // ReceitaSimples, ReceitaEspecial, Atestado, SolicitacaoExames, TermoConsentimento
    public string Title { get; private set; }
    public string Content { get; private set; }
    public string? Cid { get; private set; }
    public int? DaysOff { get; private set; }

    private MedicalDocument()
    {
        DocumentType = "ReceitaSimples";
        Title = string.Empty;
        Content = string.Empty;
    }

    public MedicalDocument(Guid patientId, Guid professionalId, string documentType,
        string title, string content, string tenantId, Guid? appointmentId = null, string? cid = null, int? daysOff = null)
    {
        PatientId = patientId;
        ProfessionalId = professionalId;
        DocumentType = documentType;
        Title = title;
        Content = content;
        TenantId = tenantId;
        AppointmentId = appointmentId;
        Cid = cid;
        DaysOff = daysOff;
        CreatedAt = DateTime.UtcNow;
    }
}
