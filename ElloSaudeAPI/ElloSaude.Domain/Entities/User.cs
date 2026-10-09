namespace ElloSaude.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; } // Profissional, Secretaria, Paciente
    public bool MustChangePassword { get; private set; }
    public Patient? Patient { get; private set; }

    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        Role = string.Empty;
    }

    public User(string email, string passwordHash, string tenantId, string role, bool mustChangePassword = false)
    {
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        TenantId = tenantId;
        Role = role;
        MustChangePassword = mustChangePassword;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MustChangePassword = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}