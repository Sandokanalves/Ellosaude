namespace ElloSaude.Domain.Entities;

public class Procedure : BaseEntity
{
    public string Name { get; private set; }
    public string Specialty { get; private set; }
    public decimal Price { get; private set; }
    public int DurationMinutes { get; private set; }
    public string ColorHex { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Procedure()
    {
        Name = string.Empty;
        Specialty = string.Empty;
        ColorHex = "#4f46e5";
    }

    public Procedure(string name, string specialty, decimal price, int durationMinutes, string colorHex, string tenantId)
    {
        Name = name;
        Specialty = specialty;
        Price = price >= 0 ? price : 0;
        DurationMinutes = durationMinutes > 0 ? durationMinutes : 30;
        ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#4f46e5" : colorHex;
        TenantId = tenantId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string specialty, decimal price, int durationMinutes, string colorHex)
    {
        Name = name;
        Specialty = specialty;
        Price = price >= 0 ? price : 0;
        DurationMinutes = durationMinutes > 0 ? durationMinutes : 30;
        ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#4f46e5" : colorHex;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ToggleStatus()
    {
        IsActive = !IsActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
