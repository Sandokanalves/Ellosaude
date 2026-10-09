using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.PatientPortal;

public sealed record GetMyPatientIdQuery : IRequest<Guid>;

public sealed class GetMyPatientIdHandler : IRequestHandler<GetMyPatientIdQuery, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetMyPatientIdHandler(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<Guid> Handle(GetMyPatientIdQuery request, CancellationToken cancellationToken)
    {
        var userId = _userService.GetUserId();
        return await _context.Patients
            .Where(patient => patient.UserId == userId && patient.IsActive)
            .Select(patient => (Guid?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("A conta não está vinculada a um paciente ativo.");
    }
}

public sealed record PatientAppointmentDto(
    Guid Id,
    Guid ProfessionalId,
    string ProfessionalName,
    DateTimeOffset Start,
    DateTimeOffset End,
    AppointmentType Type,
    AppointmentStatus Status,
    decimal Price);

public sealed record PatientPrescriptionDto(
    Guid Id,
    DateTimeOffset IssueDate,
    string ProfessionalName,
    string Crm,
    string CrmState,
    string? Notes,
    IReadOnlyCollection<PrescriptionItemDto> Items);

public sealed record PrescriptionItemDto(
    string MedicineName,
    string Dosage,
    string Frequency,
    string Duration,
    string Route,
    string? Instructions);

public sealed record GetMyAppointmentsQuery : IRequest<IReadOnlyCollection<PatientAppointmentDto>>;

public sealed record GetMyAppointmentQuery(Guid Id) : IRequest<PatientAppointmentDto>;

public sealed class GetMyAppointmentsHandler
    : IRequestHandler<GetMyAppointmentsQuery, IReadOnlyCollection<PatientAppointmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetMyAppointmentsHandler(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<IReadOnlyCollection<PatientAppointmentDto>> Handle(
        GetMyAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _userService.GetUserId();
        var patientId = await _context.Patients
            .Where(patient => patient.UserId == userId && patient.IsActive)
            .Select(patient => (Guid?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("A conta não está vinculada a um paciente ativo.");

        var appointments = await (
                from appointment in _context.Appointments
                join professional in _context.Professionals
                    on appointment.ProfessionalId equals professional.Id
                where appointment.PatientId == patientId
                orderby appointment.StartTime descending
                select new { Appointment = appointment, professional.Id, professional.Name })
            .ToListAsync(cancellationToken);

        return appointments.Select(item => new PatientAppointmentDto(
                item.Appointment.Id,
                item.Id,
                item.Name,
                new DateTimeOffset(DateTime.SpecifyKind(item.Appointment.StartTime, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(item.Appointment.EndTime, DateTimeKind.Utc)),
                item.Appointment.Type,
                item.Appointment.Status,
                item.Appointment.Price))
            .ToArray();
    }
}

public sealed class GetMyAppointmentHandler : IRequestHandler<GetMyAppointmentQuery, PatientAppointmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetMyAppointmentHandler(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<PatientAppointmentDto> Handle(
        GetMyAppointmentQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _userService.GetUserId();
        var patientId = await _context.Patients
            .Where(patient => patient.UserId == userId && patient.IsActive)
            .Select(patient => (Guid?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("A conta não está vinculada a um paciente ativo.");

        var item = await (
                from appointment in _context.Appointments
                join professional in _context.Professionals
                    on appointment.ProfessionalId equals professional.Id
                where appointment.PatientId == patientId && appointment.Id == request.Id
                select new { Appointment = appointment, professional.Id, professional.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Agendamento não encontrado.");

        return new PatientAppointmentDto(
            item.Appointment.Id,
            item.Id,
            item.Name,
            new DateTimeOffset(DateTime.SpecifyKind(item.Appointment.StartTime, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(item.Appointment.EndTime, DateTimeKind.Utc)),
            item.Appointment.Type,
            item.Appointment.Status,
            item.Appointment.Price);
    }
}

public sealed record GetMyPrescriptionsQuery : IRequest<IReadOnlyCollection<PatientPrescriptionDto>>;

public sealed class GetMyPrescriptionsHandler
    : IRequestHandler<GetMyPrescriptionsQuery, IReadOnlyCollection<PatientPrescriptionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetMyPrescriptionsHandler(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<IReadOnlyCollection<PatientPrescriptionDto>> Handle(
        GetMyPrescriptionsQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _userService.GetUserId();
        var patientId = await _context.Patients
            .Where(patient => patient.UserId == userId && patient.IsActive)
            .Select(patient => (Guid?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("A conta não está vinculada a um paciente ativo.");

        var prescriptions = await (
                from prescription in _context.Prescriptions.Include(item => item.Items)
                join professional in _context.Professionals
                    on prescription.DoctorId equals professional.Id
                where prescription.PatientId == patientId
                    && prescription.Status == PrescriptionStatus.Emitida
                orderby prescription.IssueDate descending
                select new { Prescription = prescription, Professional = professional })
            .ToListAsync(cancellationToken);

        return prescriptions.Select(item => new PatientPrescriptionDto(
                item.Prescription.Id,
                new DateTimeOffset(DateTime.SpecifyKind(item.Prescription.IssueDate, DateTimeKind.Utc)),
                item.Professional.Name,
                item.Professional.Crm,
                item.Professional.CrmState,
                item.Prescription.Notes,
                item.Prescription.Items.Select(medicine => new PrescriptionItemDto(
                    medicine.MedicineName,
                    medicine.Dosage,
                    medicine.Frequency,
                    medicine.Duration,
                    medicine.Route,
                    medicine.Instructions)).ToArray()))
            .ToArray();
    }
}
