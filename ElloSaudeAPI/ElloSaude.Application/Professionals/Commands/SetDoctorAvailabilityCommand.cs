using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Professionals.Commands;

public record SetDoctorAvailabilityCommand(
    Guid ProfessionalId,
    DayOfWeek DayOfWeek,
    TimeSpan StartTime,
    TimeSpan EndTime,
    TimeSpan? BreakStartTime,
    TimeSpan? BreakEndTime,
    int SlotDurationMinutes
) : IRequest<Guid>;

public class SetDoctorAvailabilityCommandValidator : AbstractValidator<SetDoctorAvailabilityCommand>
{
    public SetDoctorAvailabilityCommandValidator()
    {
        RuleFor(x => x.ProfessionalId).NotEmpty();
        RuleFor(x => x.DayOfWeek).IsInEnum();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("Horário de início deve ser anterior ao horário de término.");
        RuleFor(x => x.EndTime).LessThanOrEqualTo(TimeSpan.FromDays(1));
        RuleFor(x => x.SlotDurationMinutes).InclusiveBetween(10, 120).WithMessage("Duração do slot deve ser entre 10 e 120 minutos.");
        RuleFor(x => x.BreakStartTime)
            .LessThan(x => x.BreakEndTime)
            .When(x => x.BreakStartTime.HasValue && x.BreakEndTime.HasValue)
            .WithMessage("Início do intervalo deve ser anterior ao fim do intervalo.");
        RuleFor(x => x.BreakStartTime)
            .GreaterThanOrEqualTo(x => x.StartTime)
            .When(x => x.BreakStartTime.HasValue);
        RuleFor(x => x.BreakEndTime)
            .LessThanOrEqualTo(x => x.EndTime)
            .When(x => x.BreakEndTime.HasValue);
        RuleFor(x => x.BreakStartTime.HasValue)
            .Equal(x => x.BreakEndTime.HasValue)
            .WithMessage("Informe o início e o fim do intervalo.");
    }
}

public class SetDoctorAvailabilityCommandHandler : IRequestHandler<SetDoctorAvailabilityCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public SetDoctorAvailabilityCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Guid> Handle(SetDoctorAvailabilityCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetTenantId();

        var professional = await _context.Professionals
            .FirstOrDefaultAsync(p => p.Id == request.ProfessionalId && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException($"Profissional {request.ProfessionalId} não encontrado.");

        // Remove disponibilidade anterior para o mesmo dia da semana (substitui)
        var existing = _context.DoctorAvailabilities
            .Where(a => a.ProfessionalId == request.ProfessionalId && a.DayOfWeek == request.DayOfWeek);
        _context.DoctorAvailabilities.RemoveRange(existing);

        var availability = new DoctorAvailability(
            professionalId: request.ProfessionalId,
            dayOfWeek: request.DayOfWeek,
            startTime: request.StartTime,
            endTime: request.EndTime,
            tenantId: tenantId,
            breakStartTime: request.BreakStartTime,
            breakEndTime: request.BreakEndTime,
            slotDurationMinutes: request.SlotDurationMinutes
        );

        _context.DoctorAvailabilities.Add(availability);
        await _context.SaveChangesAsync(cancellationToken);

        return availability.Id;
    }
}
