using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Infrastructure.Persistence.Repositories;
using ElloSaude.Domain.Entities;

namespace ElloSaude.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Patients = new Repository<Patient>(_context);
        Appointments = new Repository<Appointment>(_context);
        Clinics = new Repository<Clinic>(_context);
        Users = new Repository<User>(_context);
        MedicalRecords = new Repository<MedicalRecord>(_context);
    }

    public IRepository<Patient> Patients { get; }
    public IRepository<Appointment> Appointments { get; }
    public IRepository<Clinic> Clinics { get; }
    public IRepository<User> Users { get; }
    public IRepository<MedicalRecord> MedicalRecords { get; }

    public async Task<int> CompleteAsync(CancellationToken ct) => await _context.SaveChangesAsync(ct);
    public void Dispose() => _context.Dispose();
}