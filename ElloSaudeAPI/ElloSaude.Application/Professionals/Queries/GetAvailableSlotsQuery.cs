using ElloSaude.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Professionals.Queries;

public record GetAvailableSlotsQuery(
    Guid ProfessionalId,
    DateOnly Date
) : IRequest<IEnumerable<TimeSlotDto>>;

public record TimeSlotDto(
    DateTime Start,
    DateTime End,
    bool IsAvailable
);

public class GetAvailableSlotsQueryHandler : IRequestHandler<GetAvailableSlotsQuery, IEnumerable<TimeSlotDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAvailableSlotsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TimeSlotDto>> Handle(GetAvailableSlotsQuery request, CancellationToken cancellationToken)
    {
        var dayOfWeek = request.Date.DayOfWeek;
        var dateAsDateTime = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Busca configuração de disponibilidade para o dia da semana
        var availability = await _context.DoctorAvailabilities
            .FirstOrDefaultAsync(a => a.ProfessionalId == request.ProfessionalId && a.DayOfWeek == dayOfWeek, cancellationToken);

        if (availability == null)
            return Enumerable.Empty<TimeSlotDto>();

        var startOfDay = dateAsDateTime.Add(availability.StartTime);
        var endOfDay = dateAsDateTime.Add(availability.EndTime);

        var existingAppointments = await _context.Appointments
            .Where(a => a.ProfessionalId == request.ProfessionalId
                && a.StartTime < endOfDay
                && a.EndTime > startOfDay
                && a.Status != Domain.Enums.AppointmentStatus.Cancelado)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(cancellationToken);
        var blocks = await _context.ScheduleBlocks
            .Where(b => b.ProfessionalId == request.ProfessionalId
                && b.IsActive
                && b.StartDateTime < endOfDay
                && b.EndDateTime > startOfDay)
            .Select(b => new { b.StartDateTime, b.EndDateTime })
            .ToListAsync(cancellationToken);

        // Gera os slots de tempo disponíveis
        var slots = new List<TimeSlotDto>();
        var slotDuration = TimeSpan.FromMinutes(availability.SlotDurationMinutes);
        var current = startOfDay;

        while (current + slotDuration <= endOfDay)
        {
            var slotEnd = current + slotDuration;

            // Verifica se o slot conflita com o intervalo de almoço.
            var isBreak = availability.BreakStartTime.HasValue && availability.BreakEndTime.HasValue
                && current < dateAsDateTime.Add(availability.BreakEndTime.Value)
                && slotEnd > dateAsDateTime.Add(availability.BreakStartTime.Value);

            var isBlocked = blocks.Any(b => b.StartDateTime < slotEnd && b.EndDateTime > current);

            var isOccupied = existingAppointments.Any(a => current < a.EndTime && slotEnd > a.StartTime);
            slots.Add(new TimeSlotDto(current, slotEnd, !isBreak && !isBlocked && !isOccupied));

            current += slotDuration;
        }

        return slots;
    }
}
