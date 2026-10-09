using AutoMapper;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using ElloSaude.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Appointments.Queries;

public record GetAgendaByProfessionalQuery(Guid ProfessionalId, DateTime Date) : IRequest<List<AppointmentDto>>;
public record GetAllAppointmentsQuery(DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<List<AppointmentDto>>;
public record GetAppointmentByIdQuery(Guid Id) : IRequest<AppointmentDto>;

public class GetAppointmentsHandler :
    IRequestHandler<GetAgendaByProfessionalQuery, List<AppointmentDto>>,
    IRequestHandler<GetAllAppointmentsQuery, List<AppointmentDto>>,
    IRequestHandler<GetAppointmentByIdQuery, AppointmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IUserService _userService;

    public GetAppointmentsHandler(
        IApplicationDbContext context,
        IMapper mapper,
        IUserService userService)
    {
        _context = context;
        _mapper = mapper;
        _userService = userService;
    }

    public async Task<List<AppointmentDto>> Handle(GetAgendaByProfessionalQuery request, CancellationToken ct)
    {
        var userRole = _userService.GetUserRole();
        var userId = _userService.GetUserId();

        var query = _context.Appointments.AsQueryable();

        if (userRole == "Profissional" && request.ProfessionalId == Guid.Empty)
        {
            query = query.Where(a => a.ProfessionalId == userId);
        }
        else if (request.ProfessionalId != Guid.Empty)
        {
            query = query.Where(a => a.ProfessionalId == request.ProfessionalId);
        }

        if (request.Date != default)
        {
            var date = request.Date.Date;
            query = query.Where(a => a.StartTime.Date == date);
        }

        var appointments = await query.ToListAsync(ct);
        return await MapToDtoListAsync(appointments, ct);
    }

    public async Task<List<AppointmentDto>> Handle(GetAllAppointmentsQuery request, CancellationToken ct)
    {
        var query = _context.Appointments.AsQueryable();

        if (request.StartDate.HasValue)
        {
            query = query.Where(a => a.StartTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(a => a.EndTime <= request.EndDate.Value);
        }

        var appointments = await query.ToListAsync(ct);
        return await MapToDtoListAsync(appointments, ct);
    }

    public async Task<AppointmentDto> Handle(GetAppointmentByIdQuery request, CancellationToken ct)
    {
        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.Id == request.Id, ct);
        if (appointment == null)
            throw new KeyNotFoundException("Agendamento não encontrado.");

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == appointment.PatientId, ct);
        var doctor = await _context.Users.FirstOrDefaultAsync(u => u.Id == appointment.ProfessionalId, ct);

        return new AppointmentDto(
            appointment.Id,
            appointment.PatientId,
            patient?.Name ?? "Paciente Desconhecido",
            appointment.ProfessionalId,
            doctor?.Email ?? "Profissional",
            appointment.StartTime,
            appointment.EndTime,
            appointment.Type,
            appointment.Status,
            appointment.TenantId
        );
    }

    private async Task<List<AppointmentDto>> MapToDtoListAsync(List<Appointment> appointments, CancellationToken ct)
    {
        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
        var doctorIds = appointments.Select(a => a.ProfessionalId).Distinct().ToList();

        var patients = await _context.Patients.Where(p => patientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var doctors = await _context.Users.Where(u => doctorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email, ct);

        return appointments.Select(a => new AppointmentDto(
            a.Id,
            a.PatientId,
            patients.GetValueOrDefault(a.PatientId, "Paciente"),
            a.ProfessionalId,
            doctors.GetValueOrDefault(a.ProfessionalId, "Profissional"),
            a.StartTime,
            a.EndTime,
            a.Type,
            a.Status,
            a.TenantId
        )).ToList();
    }
}
