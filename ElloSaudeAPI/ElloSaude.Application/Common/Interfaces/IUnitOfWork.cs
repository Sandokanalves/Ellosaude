
using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Patient> Patients { get; }
    IRepository<Appointment> Appointments { get; }
    IRepository<Clinic> Clinics { get; } // Faltava este
    IRepository<User> Users { get; }     // Faltava este
    IRepository<MedicalRecord> MedicalRecords { get; }
    Task<int> CompleteAsync(CancellationToken ct);
}