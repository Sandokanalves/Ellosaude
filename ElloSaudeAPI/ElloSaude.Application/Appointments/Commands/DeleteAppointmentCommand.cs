using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace ElloSaude.Application.Appointments.Commands;

public record DeleteAppointmentCommand(Guid Id) : IRequest;

public class DeleteAppointmentHandler : IRequestHandler<DeleteAppointmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public DeleteAppointmentHandler(IApplicationDbContext context, IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task Handle(DeleteAppointmentCommand request, CancellationToken ct)
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
            throw new UnauthorizedAccessException("Você não pode cancelar o agendamento de outro profissional.");

        if (appointment.Status is not (AppointmentStatus.Pendente or AppointmentStatus.Confirmado))
            throw new InvalidOperationException("Somente agendamentos pendentes ou confirmados podem ser cancelados.");

        appointment.Cancel("Cancelado pela equipe.");
        var financialRecord = await _context.PaymentRecords
            .FirstOrDefaultAsync(payment => payment.AppointmentId == appointment.Id, ct);
        if (financialRecord is not null
            && financialRecord.AmountPaid == 0
            && financialRecord.Status != ElloSaude.Domain.Enums.PaymentStatus.Cancelado)
            financialRecord.Cancel(userId, "Agendamento cancelado pela equipe.");

        await _context.SaveChangesAsync(ct);
    }
}
