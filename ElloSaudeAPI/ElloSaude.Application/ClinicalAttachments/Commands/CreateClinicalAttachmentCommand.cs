using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.ClinicalAttachments.Commands;

public record CreateClinicalAttachmentCommand(
    Guid PatientId,
    string Title,
    string Category,
    string ContentType,
    string FileUrl,
    string? Notes = null,
    Guid? AppointmentId = null) : IRequest<Guid>;

public class CreateClinicalAttachmentValidator : AbstractValidator<CreateClinicalAttachmentCommand>
{
    public CreateClinicalAttachmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty();
        RuleFor(x => x.FileUrl).NotEmpty();
    }
}

public class CreateClinicalAttachmentHandler : IRequestHandler<CreateClinicalAttachmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;
    private readonly IUserService _user;

    public CreateClinicalAttachmentHandler(IApplicationDbContext context, ITenantService tenant, IUserService user)
    {
        _context = context;
        _tenant = tenant;
        _user = user;
    }

    public async Task<Guid> Handle(CreateClinicalAttachmentCommand request, CancellationToken ct)
    {
        var userId = _user.GetUserId();
        var professional = await _context.Professionals
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var professionalId = professional?.Id ?? userId;

        var attachment = new ClinicalAttachment(
            request.PatientId,
            professionalId,
            request.Title,
            request.Category,
            request.ContentType,
            request.FileUrl,
            _tenant.GetTenantId(),
            request.AppointmentId,
            request.Notes
        );

        _context.ClinicalAttachments.Add(attachment);
        await _context.SaveChangesAsync(ct);
        return attachment.Id;
    }
}

public record DeleteClinicalAttachmentCommand(Guid Id) : IRequest;

public class DeleteClinicalAttachmentHandler : IRequestHandler<DeleteClinicalAttachmentCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteClinicalAttachmentHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteClinicalAttachmentCommand request, CancellationToken ct)
    {
        var attachment = await _context.ClinicalAttachments.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Anexo não encontrado.");

        _context.ClinicalAttachments.Remove(attachment);
        await _context.SaveChangesAsync(ct);
    }
}
