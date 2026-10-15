using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.MedicalDocuments.Commands;

public record CreateMedicalDocumentCommand(
    Guid PatientId,
    string DocumentType,
    string Title,
    string Content,
    string? Cid = null,
    int? DaysOff = null,
    Guid? AppointmentId = null) : IRequest<Guid>;

public class CreateMedicalDocumentValidator : AbstractValidator<CreateMedicalDocumentCommand>
{
    public CreateMedicalDocumentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DocumentType).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty();
    }
}

public class CreateMedicalDocumentHandler : IRequestHandler<CreateMedicalDocumentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;
    private readonly IUserService _user;

    public CreateMedicalDocumentHandler(IApplicationDbContext context, ITenantService tenant, IUserService user)
    {
        _context = context;
        _tenant = tenant;
        _user = user;
    }

    public async Task<Guid> Handle(CreateMedicalDocumentCommand request, CancellationToken ct)
    {
        var userId = _user.GetUserId();
        var professional = await _context.Professionals
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var professionalId = professional?.Id ?? userId;

        var document = new MedicalDocument(
            request.PatientId,
            professionalId,
            request.DocumentType,
            request.Title,
            request.Content,
            _tenant.GetTenantId(),
            request.AppointmentId,
            request.Cid,
            request.DaysOff
        );

        _context.MedicalDocuments.Add(document);
        await _context.SaveChangesAsync(ct);
        return document.Id;
    }
}
