using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.Patients.Commands;

public record CreatePatientCommand(string Name, string Email, string Cpf, DateTime BirthDate) : IRequest<Guid>;

public class CreatePatientValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cpf).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.BirthDate).NotEmpty();
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
        var patient = new Patient(request.Name, request.Email, request.Cpf, request.BirthDate, tenantId);
        await _uow.Patients.AddAsync(patient, ct);
        await _uow.CompleteAsync(ct);
        return patient.Id;
    }
}