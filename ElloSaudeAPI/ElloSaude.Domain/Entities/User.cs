namespace ElloSaude.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; } // Admin, Profissional, Secretaria, Paciente
    public bool MustChangePassword { get; private set; }
    public Patient? Patient { get; private set; }

    private User()
    {
        FullName = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
        Role = string.Empty;
    }

    public User(string email, string passwordHash, string tenantId, string role, bool mustChangePassword = false, string? fullName = null)
    {
        FullName = string.IsNullOrWhiteSpace(fullName) ? email.Split('@')[0] : fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        TenantId = tenantId;
        Role = role;
        MustChangePassword = mustChangePassword;
        CreatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MustChangePassword = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string fullName, string role)
    {
        FullName = fullName.Trim();
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsDeleted = false;
        UpdatedAt = DateTime.UtcNow;
    }
}