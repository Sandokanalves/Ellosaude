using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;

namespace ElloSaude.Application.Patients.Commands;

public record UpdatePatientCommand(
    Guid Id,
    string Name,
    string Email,
    string Cpf,
    DateTime BirthDate,
    string? Phone = null,
    string? Address = null,
    string? EmergencyContact = null,
    string? Gender = null,
    string? Notes = null,
    string? SocialName = null,
    string? Profession = null,
    string? LegalGuardian = null,
    string? MarketingOrigin = null,
    string? BloodType = null,
    string? Allergies = null,
    string? ChronicDiseases = null,
    string? ContinuousMedications = null,
    string? MedicalAlerts = null) : IRequest;

public class UpdatePatientValidator : AbstractValidator<UpdatePatientCommand>
{
    public UpdatePatientValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Cpf).NotEmpty();
        RuleFor(x => x.Phone).MaximumLength(30);
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

        patient.UpdatePersonalData(
            request.Name,
            request.Email,
            request.Cpf,
            request.BirthDate,
            request.Phone ?? patient.Phone);

        patient.Update(
            request.Name,
            request.Email,
            request.Phone ?? patient.Phone,
            request.Address ?? patient.Address,
            request.EmergencyContact ?? patient.EmergencyContact,
            request.Gender ?? patient.Gender,
            request.Notes ?? patient.Notes,
            request.SocialName ?? patient.SocialName,
            request.Profession ?? patient.Profession,
            request.LegalGuardian ?? patient.LegalGuardian,
            request.MarketingOrigin ?? patient.MarketingOrigin,
            request.BloodType ?? patient.BloodType,
            request.Allergies ?? patient.Allergies,
            request.ChronicDiseases ?? patient.ChronicDiseases,
            request.ContinuousMedications ?? patient.ContinuousMedications,
            request.MedicalAlerts ?? patient.MedicalAlerts);

        _uow.Patients.Update(patient);
        await _uow.CompleteAsync(ct);
    }
}
