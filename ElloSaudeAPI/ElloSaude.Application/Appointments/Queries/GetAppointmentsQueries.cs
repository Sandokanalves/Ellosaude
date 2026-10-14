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
        var query = _context.Appointments.AsQueryable();

        if (userRole == "Profissional")
        {
            var professionalIds = await GetCurrentProfessionalIdsAsync(ct);
            if (request.ProfessionalId != Guid.Empty && !professionalIds.Contains(request.ProfessionalId))
                throw new KeyNotFoundException("Agenda não encontrada.");

            query = query.Where(a => professionalIds.Contains(a.ProfessionalId));
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

        if (_userService.GetUserRole() == "Profissional")
        {
            var professionalIds = await GetCurrentProfessionalIdsAsync(ct);
            query = query.Where(a => professionalIds.Contains(a.ProfessionalId));
        }

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

        if (_userService.GetUserRole() == "Profissional")
        {
            var professionalIds = await GetCurrentProfessionalIdsAsync(ct);
            if (!professionalIds.Contains(appointment.ProfessionalId))
                throw new KeyNotFoundException("Agendamento não encontrado.");
        }

        var patient = await _context.Patients.FirstOrDefaultAsync(p => p.Id == appointment.PatientId, ct);
        var professional = await _context.Professionals
            .FirstOrDefaultAsync(p => p.Id == appointment.ProfessionalId, ct);

        return new AppointmentDto(
            appointment.Id,
            appointment.PatientId,
            patient?.Name ?? "Paciente Desconhecido",
            appointment.ProfessionalId,
            professional?.Name ?? "Profissional",
            appointment.StartTime,
            appointment.EndTime,
            appointment.Type,
            appointment.Status,
            appointment.TenantId
        );
    }

    private async Task<List<Guid>> GetCurrentProfessionalIdsAsync(CancellationToken ct)
    {
        var userId = _userService.GetUserId();
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("Usuário profissional autenticado inválido.");

        var professionalIds = await _context.Professionals
            .Where(professional => professional.UserId == userId && professional.IsActive)
            .Select(professional => professional.Id)
            .ToListAsync(ct);

        if (professionalIds.Count == 0)
            throw new UnauthorizedAccessException("A conta não está vinculada a um profissional ativo.");

        return professionalIds;
    }

    private async Task<List<AppointmentDto>> MapToDtoListAsync(List<Appointment> appointments, CancellationToken ct)
    {
        var patientIds = appointments.Select(a => a.PatientId).Distinct().ToList();
        var professionalIds = appointments.Select(a => a.ProfessionalId).Distinct().ToList();

        var patients = await _context.Patients.Where(p => patientIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var professionals = await _context.Professionals
            .Where(professional => professionalIds.Contains(professional.Id))
            .ToDictionaryAsync(professional => professional.Id, professional => professional.Name, ct);

        return appointments.Select(a => new AppointmentDto(
            a.Id,
            a.PatientId,
            patients.GetValueOrDefault(a.PatientId, "Paciente"),
            a.ProfessionalId,
            professionals.GetValueOrDefault(a.ProfessionalId, "Profissional"),
            a.StartTime,
            a.EndTime,
            a.Type,
            a.Status,
            a.TenantId
        )).ToList();
    }
}
