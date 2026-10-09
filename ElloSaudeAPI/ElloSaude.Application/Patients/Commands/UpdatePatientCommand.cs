using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;

namespace ElloSaude.Application.Patients.Commands;

public record UpdatePatientCommand(Guid Id, string Name, string Email, string Cpf, DateTime BirthDate) : IRequest;

public class UpdatePatientValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Cpf).NotEmpty();
    }
}

public class UpdatePatientHandler : IRequestHandler<UpdatePatientCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdatePatientHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdatePatientCommand request, CancellationToken ct)
    {
        var patient = await _uow.Patients.GetByIdAsync(request.Id, ct);
        if (patient == null)
            throw new KeyNotFoundException("Paciente não encontrado.");

        // Atualizar os dados do paciente usando reflexão ou propriedades internas
        typeof(Domain.Entities.Patient).GetProperty(nameof(patient.Name))?.SetValue(patient, request.Name);
        typeof(Domain.Entities.Patient).GetProperty(nameof(patient.Email))?.SetValue(patient, request.Email);
        typeof(Domain.Entities.Patient).GetProperty(nameof(patient.Cpf))?.SetValue(patient, request.Cpf);
        typeof(Domain.Entities.Patient).GetProperty(nameof(patient.BirthDate))?.SetValue(patient, request.BirthDate);

        _uow.Patients.Update(patient);
        await _uow.CompleteAsync(ct);
    }
}
