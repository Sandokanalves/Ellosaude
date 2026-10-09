using ElloSaude.Domain.Enums;

namespace ElloSaude.Domain.Entities;

public class PaymentRecord : BaseEntity
{
    public Guid? AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid ProfessionalId { get; private set; }
    public decimal ExpectedAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public DateTime? PaymentDate { get; private set; }
    public PaymentMethod? Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public Guid RegisteredByUserId { get; private set; }
    public string? Notes { get; private set; }

    // Propriedades de navegação (EF Core)
    public Patient? Patient { get; private set; }
    public Professional? Professional { get; private set; }

    private PaymentRecord() { }

    public PaymentRecord(Guid? appointmentId, Guid patientId, Guid professionalId,
        decimal expectedAmount, string tenantId, Guid registeredByUserId, bool isFreeReturn = false, string? notes = null)
    {
        AppointmentId = appointmentId;
        PatientId = patientId;
        ProfessionalId = professionalId;
        ExpectedAmount = isFreeReturn ? 0 : expectedAmount;
        AmountPaid = 0;
        TenantId = tenantId;
        RegisteredByUserId = registeredByUserId;
        Notes = notes;
        Status = isFreeReturn ? PaymentStatus.GratuitoRetorno : PaymentStatus.Pendente;
        CreatedAt = DateTime.UtcNow;
    }

    public void RegisterPayment(decimal amount, PaymentMethod method, Guid registeredBy, string? notes = null)
    {
        if (Status == PaymentStatus.GratuitoRetorno)
        {
            throw new InvalidOperationException("Esta consulta está marcada como retorno gratuito.");
        }
        if (Status == PaymentStatus.Cancelado)
        {
            throw new InvalidOperationException("Não é possível registrar pagamento em um lançamento cancelado.");
        }
        if (amount <= 0 || ExpectedAmount <= 0 || AmountPaid + amount > ExpectedAmount)
        {
            throw new InvalidOperationException("O valor informado excede o saldo previsto ou não é válido.");
        }
        if (AmountPaid > 0 && Method != method)
        {
            throw new InvalidOperationException("Um lançamento não pode combinar formas de pagamento diferentes.");
        }

        AmountPaid += amount;
        Method = method;
        PaymentDate = DateTime.UtcNow;
        RegisteredByUserId = registeredBy;
        Status = AmountPaid >= ExpectedAmount ? PaymentStatus.Pago : PaymentStatus.ParcialmentePago;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            Notes = notes;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFreeReturn(Guid registeredBy, string? reason = null)
    {
        if (Status != PaymentStatus.Pendente || AmountPaid != 0)
        {
            throw new InvalidOperationException("Somente consultas pendentes e sem pagamentos podem ser marcadas como retorno gratuito.");
        }

        ExpectedAmount = 0;
        AmountPaid = 0;
        PaymentDate = null;
        Method = null;
        Status = PaymentStatus.GratuitoRetorno;
        RegisteredByUserId = registeredBy;
        Notes = reason ?? "Consulta de retorno sem cobrança.";
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(Guid registeredBy, string reason)
    {
        if (AmountPaid > 0)
        {
            throw new InvalidOperationException("Um lançamento com pagamentos recebidos precisa de estorno antes do cancelamento.");
        }
        if (Status == PaymentStatus.Cancelado)
        {
            throw new InvalidOperationException("O lançamento já está cancelado.");
        }

        Status = PaymentStatus.Cancelado;
        RegisteredByUserId = registeredBy;
        Notes = $"Cancelado: {reason}";
        UpdatedAt = DateTime.UtcNow;
    }
}
