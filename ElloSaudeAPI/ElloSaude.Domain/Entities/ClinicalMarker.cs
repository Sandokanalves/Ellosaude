namespace ElloSaude.Domain.Entities;

public class ClinicalMarker : BaseEntity
{
    public Guid PatientId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public Guid ProfessionalId { get; private set; }
    public string Target { get; private set; } // BodyFront, BodyBack, Face
    public decimal X { get; private set; } // 0 - 100%
    public decimal Y { get; private set; } // 0 - 100%
    public string Type { get; private set; } // Dor, Lesao, Toxina, Preenchimento, Outro
    public string Label { get; private set; }
    public string? IntensityOrDose { get; private set; }
    public string? Notes { get; private set; }

    private ClinicalMarker()
    {
        Target = "Face";
        Type = "Toxina";
        Label = string.Empty;
    }

    public ClinicalMarker(Guid patientId, Guid professionalId, string target, decimal x, decimal y,
        string type, string label, string tenantId, Guid? appointmentId = null, string? intensityOrDose = null, string? notes = null)
    {
        PatientId = patientId;
        ProfessionalId = professionalId;
        Target = target;
        X = x;
        Y = y;
        Type = type;
        Label = label;
        TenantId = tenantId;
        AppointmentId = appointmentId;
        IntensityOrDose = intensityOrDose;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }
}
