using ElloSaude.Application.Common.Interfaces;
using MediatR;

namespace ElloSaude.Application.Appointments.Commands;

public record DeleteAppointmentCommand(Guid Id) : IRequest;

public class DeleteAppointmentHandler : IRequestHandler<DeleteAppointmentCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteAppointmentHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(DeleteAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await _uow.Appointments.GetByIdAsync(request.Id, ct);
        if (appointment == null)
            throw new KeyNotFoundException("Agendamento não encontrado.");

        _uow.Appointments.Delete(appointment);
        await _uow.CompleteAsync(ct);
    }
}
