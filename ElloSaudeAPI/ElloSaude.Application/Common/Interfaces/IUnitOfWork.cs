using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Patient> Patients { get; }
    IRepository<Appointment> Appointments { get; }
    IRepository<Clinic> Clinics { get; }
    IRepository<User> Users { get; }
    IRepository<MedicalRecord> MedicalRecords { get; }
    IRepository<Professional> Professionals { get; }
    IRepository<DoctorAvailability> DoctorAvailabilities { get; }
    IRepository<ScheduleBlock> ScheduleBlocks { get; }
    IRepository<Prescription> Prescriptions { get; }
    IRepository<PaymentRecord> PaymentRecords { get; }
    IRepository<AuditLog> AuditLogs { get; }
    Task<int> CompleteAsync(CancellationToken ct);
}