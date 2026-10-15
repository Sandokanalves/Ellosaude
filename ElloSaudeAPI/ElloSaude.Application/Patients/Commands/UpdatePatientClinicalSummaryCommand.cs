using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;

namespace ElloSaude.Application.Patients.Commands;

public record UpdatePatientClinicalSummaryCommand(
    Guid PatientId,
    string? BloodType,
    string? Allergies,
    string? ChronicDiseases,
    string? ContinuousMedications,
    string? MedicalAlerts) : IRequest;

public class UpdatePatientClinicalSummaryValidator : AbstractValidator<UpdatePatientClinicalSummaryCommand>
{
    public UpdatePatientClinicalSummaryValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
    }
}

public class UpdatePatientClinicalSummaryHandler : IRequestHandler<UpdatePatientClinicalSummaryCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdatePatientClinicalSummaryHandler(IUnitOfWork uow) => _uow = uow;

    public async Task Handle(UpdatePatientClinicalSummaryCommand request, CancellationToken ct)
    {
        var patient = await _uow.Patients.GetByIdAsync(request.PatientId, ct)
            ?? throw new KeyNotFoundException("Paciente não encontrado.");

        patient.UpdateClinicalSummary(
            request.BloodType,
            request.Allergies,
            request.ChronicDiseases,
            request.ContinuousMedications,
            request.MedicalAlerts);

        _uow.Patients.Update(patient);
        await _uow.CompleteAsync(ct);
    }
}
