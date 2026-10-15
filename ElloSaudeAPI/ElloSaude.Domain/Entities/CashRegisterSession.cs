namespace ElloSaude.Domain.Entities;

public class CashRegisterSession : BaseEntity
{
    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal? ClosingBalance { get; private set; }
    public string Status { get; private set; } // Aberto, Fechado
    public Guid OpenedByUserId { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public string? Notes { get; private set; }

    private CashRegisterSession()
    {
        Status = "Aberto";
    }

    public CashRegisterSession(decimal openingBalance, Guid openedByUserId, string tenantId, string? notes = null)
    {
        OpeningBalance = openingBalance >= 0 ? openingBalance : 0;
        OpenedByUserId = openedByUserId;
        TenantId = tenantId;
        OpenedAt = DateTime.UtcNow;
        Status = "Aberto";
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }

    public void Close(decimal closingBalance, Guid closedByUserId, string? notes = null)
    {
        if (Status != "Aberto")
            throw new InvalidOperationException("Este caixa já foi fechado.");

        ClosingBalance = closingBalance;
        ClosedByUserId = closedByUserId;
        ClosedAt = DateTime.UtcNow;
        Status = "Fechado";
        if (!string.IsNullOrWhiteSpace(notes))
            Notes = string.IsNullOrWhiteSpace(Notes) ? notes : $"{Notes} | Fechamento: {notes}";
        UpdatedAt = DateTime.UtcNow;
    }
}
