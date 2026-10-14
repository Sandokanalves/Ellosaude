using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Appointments.Queries;

public sealed record AppointmentStatusHistoryDto(
    Guid Id,
    AppointmentStatus PreviousStatus,
    AppointmentStatus NewStatus,
    Guid? ChangedByUserId,
    string ChangedByRole,
    DateTime OccurredAt);

public sealed record GetAppointmentStatusHistoryQuery(Guid AppointmentId)
    : IRequest<IReadOnlyList<AppointmentStatusHistoryDto>>;

public sealed class GetAppointmentStatusHistoryHandler
    : IRequestHandler<GetAppointmentStatusHistoryQuery, IReadOnlyList<AppointmentStatusHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetAppointmentStatusHistoryHandler(
        IApplicationDbContext context,
        IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<IReadOnlyList<AppointmentStatusHistoryDto>> Handle(
        GetAppointmentStatusHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var appointment = await _context.Appointments
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.AppointmentId, cancellationToken);
        if (appointment is null)
            throw new KeyNotFoundException("Agendamento não encontrado.");

        if (_userService.GetUserRole() == "Profissional")
        {
            var userId = _userService.GetUserId();
            var canViewHistory = userId != Guid.Empty
                && await _context.Professionals.AnyAsync(
                    professional => professional.Id == appointment.ProfessionalId
                        && professional.UserId == userId
                        && professional.IsActive,
                    cancellationToken);
            if (!canViewHistory)
                throw new KeyNotFoundException("Agendamento não encontrado.");
        }

        return await _context.AppointmentStatusHistory
            .AsNoTracking()
            .Where(history => history.AppointmentId == appointment.Id)
            .OrderBy(history => history.OccurredAt)
            .ThenBy(history => history.Id)
            .Select(history => new AppointmentStatusHistoryDto(
                history.Id,
                history.PreviousStatus,
                history.NewStatus,
                history.ChangedByUserId,
                history.ChangedByRole,
                history.OccurredAt))
            .ToListAsync(cancellationToken);
    }
}
