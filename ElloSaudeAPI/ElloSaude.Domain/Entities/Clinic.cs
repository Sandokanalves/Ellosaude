namespace ElloSaude.Domain.Entities;

public class Clinic : BaseEntity
{
    public string Name { get; private set; }
    public string Cnpj { get; private set; }
    public string NameFantasy { get; private set; }

    public Clinic(string name, string cnpj, string tenantId)
    {
        Name = name;
        Cnpj = cnpj;
        NameFantasy = name;
        TenantId = tenantId; // RNF04 - Cada entidade deve ter o TenantId para garantir a segregação de dados entre as clínicas (SaaS) [cite: 27]
    }
}