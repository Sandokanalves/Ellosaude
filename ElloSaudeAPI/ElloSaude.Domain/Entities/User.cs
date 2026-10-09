namespace ElloSaude.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string Role { get; private set; } // Profissional, Secretaria, Paciente

    public User(string email, string passwordHash, string tenantId, string role)
    {
        Email = email;
        PasswordHash = passwordHash;
        TenantId = tenantId;
        Role = role;
    }
}