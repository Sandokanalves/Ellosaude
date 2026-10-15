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
        Professionals = new Repository<Professional>(_context);
        DoctorAvailabilities = new Repository<DoctorAvailability>(_context);
        ScheduleBlocks = new Repository<ScheduleBlock>(_context);
        Prescriptions = new Repository<Prescription>(_context);
        PaymentRecords = new Repository<PaymentRecord>(_context);
        AuditLogs = new Repository<AuditLog>(_context);
        Procedures = new Repository<Procedure>(_context);
        ClinicalAttachments = new Repository<ClinicalAttachment>(_context);
        ClinicalMarkers = new Repository<ClinicalMarker>(_context);
        MedicalDocuments = new Repository<MedicalDocument>(_context);
        ClinicExpenses = new Repository<ClinicExpense>(_context);
        CashRegisterSessions = new Repository<CashRegisterSession>(_context);
    }

    public IRepository<Patient> Patients { get; }
    public IRepository<Appointment> Appointments { get; }
    public IRepository<Clinic> Clinics { get; }
    public IRepository<User> Users { get; }
    public IRepository<MedicalRecord> MedicalRecords { get; }
    public IRepository<Professional> Professionals { get; }
    public IRepository<DoctorAvailability> DoctorAvailabilities { get; }
    public IRepository<ScheduleBlock> ScheduleBlocks { get; }
    public IRepository<Prescription> Prescriptions { get; }
    public IRepository<PaymentRecord> PaymentRecords { get; }
    public IRepository<AuditLog> AuditLogs { get; }
    public IRepository<Procedure> Procedures { get; }
    public IRepository<ClinicalAttachment> ClinicalAttachments { get; }
    public IRepository<ClinicalMarker> ClinicalMarkers { get; }
    public IRepository<MedicalDocument> MedicalDocuments { get; }
    public IRepository<ClinicExpense> ClinicExpenses { get; }
    public IRepository<CashRegisterSession> CashRegisterSessions { get; }

    public async Task<int> CompleteAsync(CancellationToken ct) => await _context.SaveChangesAsync(ct);
    public void Dispose() => _context.Dispose();
}