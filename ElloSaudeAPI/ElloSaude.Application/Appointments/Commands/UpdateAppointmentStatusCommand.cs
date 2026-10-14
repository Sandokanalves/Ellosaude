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
    private readonly IWaitingRoomNotifier _waitingRoomNotifier;

    public UpdateAppointmentStatusHandler(
        IApplicationDbContext context,
        IUserService userService,
        IWaitingRoomNotifier waitingRoomNotifier)
    {
        _context = context;
        _userService = userService;
        _waitingRoomNotifier = waitingRoomNotifier;
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

        var shouldNotifyArrival = request.Status == AppointmentStatus.AguardandoRecepcao
            && appointment.Status != AppointmentStatus.AguardandoRecepcao;
        Guid? professionalUserId = null;
        if (shouldNotifyArrival)
        {
            professionalUserId = await _context.Professionals
                .Where(professional => professional.Id == appointment.ProfessionalId
                    && professional.IsActive)
                .Select(professional => professional.UserId)
                .FirstOrDefaultAsync(ct);

            if (professionalUserId is null)
                throw new InvalidOperationException(
                    "O profissional do agendamento não possui uma conta ativa vinculada para receber notificações.");
        }

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

        if (shouldNotifyArrival && professionalUserId.HasValue)
        {
            await _waitingRoomNotifier.NotifyPatientArrivedAsync(
                professionalUserId.Value,
                appointment.Id,
                new DateTimeOffset(
                    DateTime.SpecifyKind(appointment.ArrivedAt!.Value, DateTimeKind.Utc)),
                ct);
        }
    }
}
