using ElloSaude.Domain.Enums;

namespace ElloSaude.Domain.Entities;

public class Prescription : BaseEntity
{
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid? AppointmentId { get; private set; }
    public DateTime IssueDate { get; private set; }
    public PrescriptionStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public string? HashSignature { get; private set; }
    public ICollection<PrescriptionItem> Items { get; private set; }

    private Prescription()
    {
        Items = new List<PrescriptionItem>();
    }

    public Prescription(Guid patientId, Guid doctorId, string tenantId, Guid? appointmentId = null, string? notes = null)
    {
        PatientId = patientId;
        DoctorId = doctorId;
        AppointmentId = appointmentId;
        TenantId = tenantId;
        IssueDate = DateTime.UtcNow;
        Status = PrescriptionStatus.Rascunho;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
        Items = new List<PrescriptionItem>();
    }

    public void AddItem(string medicineName, string dosage, string frequency, string duration, string route, string? instructions)
    {
        if (Status != PrescriptionStatus.Rascunho)
        {
            throw new InvalidOperationException("Não é possível adicionar itens a uma receita já emitida ou cancelada.");
        }

        Items.Add(new PrescriptionItem(Id, medicineName, dosage, frequency, duration, route, instructions, TenantId!));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Finalize(string hashSignature)
    {
        if (Items.Count == 0)
        {
            throw new InvalidOperationException("Não é possível emitir uma receita sem medicamentos adicionados.");
        }

        Status = PrescriptionStatus.Emitida;
        HashSignature = hashSignature;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = PrescriptionStatus.Cancelada;
        UpdatedAt = DateTime.UtcNow;
    }
}
