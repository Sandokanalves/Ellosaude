using ElloSaude.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ElloSaude.Infrastructure.Persistence;

public sealed class ApplicationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost;Database=ElloSaudeDesignTime;Integrated Security=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ApplicationDbContext(options, new DesignTimeTenantService());
    }

    private sealed class DesignTimeTenantService : ITenantService
    {
        public string GetTenantId() => string.Empty;
        public Guid GetUserId() => Guid.Empty;
    }
}
