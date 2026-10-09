using MediatR;
using FluentValidation;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;

namespace ElloSaude.Application.Appointments.Commands;

public record CreateAppointmentCommand(
    Guid PatientId,
    Guid ProfessionalId,
    DateTime Start,
    DateTime End,
    int TypeId) : IRequest<Guid>;

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Start).GreaterThan(DateTime.Now).WithMessage("Data deve ser futura."); // RF03 [cite: 8]
        RuleFor(x => x.End).GreaterThan(x => x.Start);
    }
}

public class CreateAppointmentHandler : IRequestHandler<CreateAppointmentCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ITenantService _tenant;

    public CreateAppointmentHandler(IUnitOfWork uow, ITenantService tenant)
    {
        _uow = uow;
        _tenant = tenant;
    }

    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken ct)
    {
        var appointment = new Appointment(
            request.Start,
            request.End,
            request.PatientId,
            request.ProfessionalId,
            (AppointmentType)request.TypeId,
            _tenant.GetTenantId());

        await _uow.Appointments.AddAsync(appointment, ct);
        await _uow.CompleteAsync(ct);
        return appointment.Id;
    }
}