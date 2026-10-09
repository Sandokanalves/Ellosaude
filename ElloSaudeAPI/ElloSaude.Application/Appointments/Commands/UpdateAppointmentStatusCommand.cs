using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Appointments.Commands;

public record UpdateAppointmentStatusCommand(Guid Id, AppointmentStatus Status) : IRequest;

public class UpdateAppointmentStatusValidator : AbstractValidator<UpdateAppointmentStatusCommand>
{
    public UpdateAppointmentStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateAppointmentStatusHandler : IRequestHandler<UpdateAppointmentStatusCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public UpdateAppointmentStatusHandler(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task Handle(UpdateAppointmentStatusCommand request, CancellationToken ct)
    {
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(item => item.Id == request.Id, ct);
        if (appointment == null)
            throw new KeyNotFoundException("Agendamento não encontrado.");

        var role = _userService.GetUserRole();
        var userId = _userService.GetUserId();
        if (role == "Profissional"
            && !await _context.Professionals.AnyAsync(
                professional => professional.Id == appointment.ProfessionalId
                    && professional.UserId == userId,
                ct))
            throw new UnauthorizedAccessException("Você não pode alterar o agendamento de outro profissional.");

        appointment.UpdateStatus(request.Status);
        if (request.Status == AppointmentStatus.Cancelado)
        {
            var payment = await _context.PaymentRecords
                .FirstOrDefaultAsync(record => record.AppointmentId == appointment.Id, ct);
            if (payment is not null
                && payment.AmountPaid == 0
                && payment.Status != ElloSaude.Domain.Enums.PaymentStatus.Cancelado)
                payment.Cancel(userId, "Agendamento cancelado pela equipe.");
        }
        await _context.SaveChangesAsync(ct);
    }
}
