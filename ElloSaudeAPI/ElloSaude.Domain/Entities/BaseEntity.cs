namespace ElloSaude.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string? TenantId { get; set; } // Identificador da Clínica (SaaS) [cite: 27]
}