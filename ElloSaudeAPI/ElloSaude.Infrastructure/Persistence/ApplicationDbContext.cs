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
    public DbSet<MedicalRecordAddendum> MedicalRecordAddenda => Set<MedicalRecordAddendum>();
    public DbSet<Professional> Professionals => Set<Professional>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<ScheduleBlock> ScheduleBlocks => Set<ScheduleBlock>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<PaymentRecord> PaymentRecords => Set<PaymentRecord>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Propriedade usada na compilação do Query Filter do EF Core
    public string CurrentTenantId => _tenantService?.GetTenantId() ?? string.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Configurações de Precisão Monetária
        modelBuilder.Entity<Appointment>()
            .Property(a => a.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Professional>()
            .Property(p => p.ConsultationPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PaymentRecord>()
            .Property(pr => pr.ExpectedAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<PaymentRecord>()
            .Property(pr => pr.AmountPaid)
            .HasPrecision(18, 2);

        // 2. Relacionamentos
        modelBuilder.Entity<MedicalRecord>()
            .HasMany(m => m.Addenda)
            .WithOne()
            .HasForeignKey(a => a.MedicalRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Prescription>()
            .HasMany(p => p.Items)
            .WithOne()
            .HasForeignKey(i => i.PrescriptionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Professional>()
            .HasMany(p => p.Availabilities)
            .WithOne()
            .HasForeignKey(a => a.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Professional>()
            .HasMany(p => p.ScheduleBlocks)
            .WithOne()
            .HasForeignKey(b => b.ProfessionalId)
            .OnDelete(DeleteBehavior.Cascade);

        // 3. Índices por Tenant para performance e isolamento
        modelBuilder.Entity<Patient>()
            .HasIndex(p => new { p.TenantId, p.Cpf });

        modelBuilder.Entity<Patient>()
            .HasIndex(p => new { p.TenantId, p.Name });

        modelBuilder.Entity<Appointment>()
            .HasIndex(a => new { a.TenantId, a.ProfessionalId, a.StartTime });

        modelBuilder.Entity<Professional>()
            .HasIndex(p => new { p.TenantId, p.Crm, p.CrmState });

        modelBuilder.Entity<PaymentRecord>()
            .HasIndex(pr => new { pr.TenantId, pr.PaymentDate, pr.Status });

        modelBuilder.Entity<AuditLog>()
            .HasIndex(al => new { al.TenantId, al.CreatedAt });

        // 4. Aplica o filtro global de tenant dinâmico em entidades que herdam de BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType) && entityType.ClrType != typeof(Clinic))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, "TenantId");
                var currentTenantId = Expression.Property(
                    Expression.Constant(this),
                    nameof(CurrentTenantId));
                var isNotDeleted = Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));
                var tenantMatches = Expression.Equal(property, currentTenantId);
                var condition = Expression.AndAlso(tenantMatches, isNotDeleted);
                var lambda = Expression.Lambda(condition, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantService.GetTenantId();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (string.IsNullOrWhiteSpace(entry.Entity.TenantId))
                {
                    entry.Entity.TenantId = tenantId;
                }
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(ct);
    }
}