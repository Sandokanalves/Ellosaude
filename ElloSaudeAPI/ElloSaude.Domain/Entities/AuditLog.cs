namespace ElloSaude.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string UserEmail { get; private set; }
    public string UserRole { get; private set; }
    public string Action { get; private set; }
    public string EntityName { get; private set; }
    public string EntityId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Details { get; private set; }

    private AuditLog()
    {
        UserEmail = string.Empty;
        UserRole = string.Empty;
        Action = string.Empty;
        EntityName = string.Empty;
        EntityId = string.Empty;
    }

    public AuditLog(Guid? userId, string userEmail, string userRole, string action,
        string entityName, string entityId, string tenantId, string? ipAddress = null, string? details = null)
    {
        UserId = userId;
        UserEmail = userEmail;
        UserRole = userRole;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        TenantId = tenantId;
        IpAddress = ipAddress;
        Details = details;
        CreatedAt = DateTime.UtcNow;
    }
}
