using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ElloSaude.Application.MedicalRecords.Commands;

public record AddMedicalRecordAddendumCommand(Guid MedicalRecordId, string Note) : IRequest;

public class AddMedicalRecordAddendumCommandValidator : AbstractValidator<AddMedicalRecordAddendumCommand>
{
    public AddMedicalRecordAddendumCommandValidator()
    {
        RuleFor(x => x.MedicalRecordId).NotEmpty();
        RuleFor(x => x.Note).NotEmpty().MaximumLength(4000);
    }
}

public class AddMedicalRecordAddendumCommandHandler
    : IRequestHandler<AddMedicalRecordAddendumCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;

    public AddMedicalRecordAddendumCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantService tenantService,
        IUserService userService)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _userService = userService;
    }

    public async Task Handle(
        AddMedicalRecordAddendumCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _userService.GetUserId();
        var record = await _unitOfWork.MedicalRecords.GetByIdAsync(request.MedicalRecordId, cancellationToken)
            ?? throw new KeyNotFoundException("Prontuário não encontrado.");

        record.AddAddendum(userId, request.Note, _tenantService.GetTenantId());
        _unitOfWork.MedicalRecords.Update(record);

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        await _unitOfWork.AuditLogs.AddAsync(new AuditLog(
            userId,
            user?.Email ?? string.Empty,
            _userService.GetUserRole(),
            "Adicionar evolução ao prontuário",
            nameof(MedicalRecord),
            record.Id.ToString(),
            _tenantService.GetTenantId()
        ), cancellationToken);

        await _unitOfWork.CompleteAsync(cancellationToken);
    }
}
