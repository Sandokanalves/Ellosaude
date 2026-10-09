using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ElloSaude.Application.Appointments.Commands;

public record UpdateAppointmentStatusCommand(Guid Id, AppointmentStatus Status) : IRequest;

public class UpdateAppointmentStatusValidator : AbstractValidator<UpdateAppointmentStatusCommand>
{
    public UpdateAppointmentStatusValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class UpdateAppointmentStatusHandler : IRequestHandler<UpdateAppointmentStatusCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateAppointmentStatusHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdateAppointmentStatusCommand request, CancellationToken ct)
    {
        var appointment = await _uow.Appointments.GetByIdAsync(request.Id, ct);
        if (appointment == null)
            throw new KeyNotFoundException("Agendamento não encontrado.");

        typeof(Domain.Entities.Appointment)
            .GetProperty(nameof(appointment.Status))
            ?.SetValue(appointment, request.Status);

        _uow.Appointments.Update(appointment);
        await _uow.CompleteAsync(ct);
    }
}
