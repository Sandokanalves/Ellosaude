using Microsoft.EntityFrameworkCore;
using ElloSaude.Domain.Entities;
using ElloSaude.Application.Common.Interfaces;
using System.Linq.Expressions;

namespace ElloSaude.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly ITenantService _tenantService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<User> Users => Set<User>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplica o filtro de tenant dinâmico em todas as entidades que herdam de BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, "TenantId");

                // Invoca _tenantService.GetTenantId() dinamicamente a cada consulta
                var tenantServiceInstance = Expression.Constant(_tenantService);
                var getTenantIdCall = Expression.Call(tenantServiceInstance, typeof(ITenantService).GetMethod(nameof(ITenantService.GetTenantId))!);
                var condition = Expression.Equal(property, getTenantIdCall);
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(entry.Entity.TenantId))
                {
                    entry.Entity.TenantId = _tenantService.GetTenantId();
                }
            }
        }
        return base.SaveChangesAsync(ct);
    }
}