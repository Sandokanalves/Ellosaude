using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ElloSaude.Application.Common.Interfaces;

namespace ElloSaude.Infrastructure.Identity;

public class TenantService : ITenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetTenantId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return string.Empty;

        return user.FindFirst("TenantId")?.Value
            ?? user.FindFirst(c => c.Type.Equals("TenantId", StringComparison.OrdinalIgnoreCase))?.Value
            ?? string.Empty;
    }

    public Guid GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null) return Guid.Empty;

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;

        return userId != null ? Guid.Parse(userId) : Guid.Empty;
    }
}