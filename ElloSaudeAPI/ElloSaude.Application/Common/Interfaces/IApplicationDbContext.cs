using Microsoft.EntityFrameworkCore;
using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Patient> Patients { get; }
    DbSet<Appointment> Appointments { get; }
    DbSet<Clinic> Clinics { get; }
    DbSet<User> Users { get; }
    DbSet<MedicalRecord> MedicalRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
