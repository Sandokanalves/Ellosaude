using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.Patients.Commands;

public record CreatePatientCommand(
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
    string? MedicalAlerts = null) : IRequest<Guid>;

public class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cpf).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.BirthDate).NotEmpty();
        RuleFor(x => x.Phone).MaximumLength(30);
    }
}

public class CreatePatientHandler : IRequestHandler<CreatePatientCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ITenantService _tenant;

    public CreatePatientHandler(IUnitOfWork uow, ITenantService tenant)
    {
        _uow = uow;
        _tenant = tenant;
    }

    public async Task<Guid> Handle(CreatePatientCommand request, CancellationToken ct)
    {
        var tenantId = _tenant.GetTenantId();
        var patient = new Patient(
            request.Name,
            request.Email,
            request.Cpf,
            request.BirthDate,
            tenantId,
            phone: request.Phone,
            address: request.Address,
            emergencyContact: request.EmergencyContact,
            gender: request.Gender,
            notes: request.Notes,
            socialName: request.SocialName,
            profession: request.Profession,
            legalGuardian: request.LegalGuardian,
            marketingOrigin: request.MarketingOrigin,
            bloodType: request.BloodType,
            allergies: request.Allergies,
            chronicDiseases: request.ChronicDiseases,
            continuousMedications: request.ContinuousMedications,
            medicalAlerts: request.MedicalAlerts);

        await _uow.Patients.AddAsync(patient, ct);
        await _uow.CompleteAsync(ct);
        return patient.Id;
    }
}