namespace ElloSaude.Domain.Entities;

public class PrescriptionItem : BaseEntity
{
    public Guid PrescriptionId { get; private set; }
    public string MedicineName { get; private set; }
    public string Dosage { get; private set; }
    public string Frequency { get; private set; }
    public string Duration { get; private set; }
    public string Route { get; private set; }
    public string? Instructions { get; private set; }

    private PrescriptionItem()
    {
        MedicineName = string.Empty;
        Dosage = string.Empty;
        Frequency = string.Empty;
        Duration = string.Empty;
        Route = string.Empty;
    }

    public PrescriptionItem(Guid prescriptionId, string medicineName, string dosage,
        string frequency, string duration, string route, string? instructions, string tenantId)
    {
        PrescriptionId = prescriptionId;
        MedicineName = medicineName;
        Dosage = dosage;
        Frequency = frequency;
        Duration = duration;
        Route = route;
        Instructions = instructions;
        TenantId = tenantId;
        CreatedAt = DateTime.UtcNow;
    }
}
