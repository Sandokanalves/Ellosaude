namespace ElloSaude.Domain.Entities;

public class ClinicExpense : BaseEntity
{
    public string Description { get; private set; }
    public string Category { get; private set; } // Aluguel, Energia, Insumos, Folha, Marketing, Outros
    public decimal Amount { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? PaidDate { get; private set; }
    public bool IsPaid { get; private set; }
    public string? PaymentMethod { get; private set; }
    public string? Notes { get; private set; }

    private ClinicExpense()
    {
        Description = string.Empty;
        Category = "Outros";
    }

    public ClinicExpense(string description, string category, decimal amount, DateTime dueDate,
        string tenantId, bool isPaid = false, DateTime? paidDate = null, string? paymentMethod = null, string? notes = null)
    {
        Description = description;
        Category = category;
        Amount = amount >= 0 ? amount : 0;
        DueDate = dueDate;
        TenantId = tenantId;
        IsPaid = isPaid;
        PaidDate = isPaid ? (paidDate ?? DateTime.UtcNow) : null;
        PaymentMethod = paymentMethod;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid(DateTime? paidDate = null, string? paymentMethod = null)
    {
        IsPaid = true;
        PaidDate = paidDate ?? DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(paymentMethod))
            PaymentMethod = paymentMethod;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string description, string category, decimal amount, DateTime dueDate, string? notes = null)
    {
        Description = description;
        Category = category;
        Amount = amount >= 0 ? amount : 0;
        DueDate = dueDate;
        Notes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
