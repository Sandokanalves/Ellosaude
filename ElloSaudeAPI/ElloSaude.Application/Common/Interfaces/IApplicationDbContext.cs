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
    DbSet<MedicalRecordAddendum> MedicalRecordAddenda { get; }
    DbSet<Professional> Professionals { get; }
    DbSet<DoctorAvailability> DoctorAvailabilities { get; }
    DbSet<ScheduleBlock> ScheduleBlocks { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<PrescriptionItem> PrescriptionItems { get; }
    DbSet<PaymentRecord> PaymentRecords { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
