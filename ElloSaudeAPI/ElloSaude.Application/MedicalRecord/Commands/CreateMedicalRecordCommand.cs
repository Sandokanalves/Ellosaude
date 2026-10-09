using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.MedicalRecords.Commands;

public record CreateMedicalRecordCommand(Guid PatientId, string Description, string Diagnosis) : IRequest<Guid>;

public class CreateMedicalRecordValidator : AbstractValidator<CreateMedicalRecordCommand>
{
    public CreateMedicalRecordValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Diagnosis).NotEmpty();
    }
}

public class CreateMedicalRecordHandler : IRequestHandler<CreateMedicalRecordCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ITenantService _tenant;
    private readonly IUserService _user;

    public CreateMedicalRecordHandler(IUnitOfWork uow, ITenantService tenant, IUserService user)
    {
        _uow = uow;
        _tenant = tenant;
        _user = user;
    }

    public async Task<Guid> Handle(CreateMedicalRecordCommand request, CancellationToken ct)
    {
        var record = new MedicalRecord(
            request.PatientId,
            _user.GetUserId(),
            request.Description,
            request.Diagnosis,
            _tenant.GetTenantId()
        );

        await _uow.MedicalRecords.AddAsync(record, ct);
        await _uow.CompleteAsync(ct);
        return record.Id;
    }
}