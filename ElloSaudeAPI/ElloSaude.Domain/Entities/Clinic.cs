namespace ElloSaude.Domain.Entities;

public class Clinic : BaseEntity
{
    public string Name { get; private set; }
    public string Cnpj { get; private set; }
    public string NameFantasy { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? LogoUrl { get; private set; }
    public int MaxReturnDays { get; private set; } = 30;
    public decimal DefaultCommissionPercentage { get; private set; } = 50.0m;

    private Clinic()
    {
        Name = string.Empty;
        Cnpj = string.Empty;
        NameFantasy = string.Empty;
    }

    public Clinic(string name, string cnpj, string tenantId, string? nameFantasy = null,
        string? phone = null, string? email = null, string? address = null,
        string? city = null, string? state = null, string? logoUrl = null,
        int maxReturnDays = 30, decimal defaultCommissionPercentage = 50.0m)
    {
        Name = name;
        Cnpj = cnpj;
        NameFantasy = string.IsNullOrWhiteSpace(nameFantasy) ? name : nameFantasy;
        TenantId = tenantId;
        Phone = phone;
        Email = email;
        Address = address;
        City = city;
        State = state;
        LogoUrl = logoUrl;
        MaxReturnDays = maxReturnDays > 0 ? maxReturnDays : 30;
        DefaultCommissionPercentage = defaultCommissionPercentage >= 0 ? defaultCommissionPercentage : 50.0m;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateSettings(string name, string cnpj, string nameFantasy,
        string? phone, string? email, string? address, string? city, string? state,
        string? logoUrl, int maxReturnDays, decimal defaultCommissionPercentage)
    {
        Name = name;
        Cnpj = cnpj;
        NameFantasy = string.IsNullOrWhiteSpace(nameFantasy) ? name : nameFantasy;
        Phone = phone;
        Email = email;
        Address = address;
        City = city;
        State = state;
        LogoUrl = logoUrl;
        MaxReturnDays = maxReturnDays > 0 ? maxReturnDays : 30;
        DefaultCommissionPercentage = defaultCommissionPercentage >= 0 ? defaultCommissionPercentage : 50.0m;
        UpdatedAt = DateTime.UtcNow;
    }
}