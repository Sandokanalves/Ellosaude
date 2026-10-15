using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.ClinicalMarkers.Commands;

public record SaveClinicalMarkerCommand(
    Guid PatientId,
    string Target,
    decimal X,
    decimal Y,
    string Type,
    string Label,
    string? IntensityOrDose = null,
    string? Notes = null,
    Guid? AppointmentId = null) : IRequest<Guid>;

public class SaveClinicalMarkerValidator : AbstractValidator<SaveClinicalMarkerCommand>
{
    public SaveClinicalMarkerValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Target).NotEmpty();
        RuleFor(x => x.X).InclusiveBetween(0, 100);
        RuleFor(x => x.Y).InclusiveBetween(0, 100);
        RuleFor(x => x.Type).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(150);
    }
}

public class SaveClinicalMarkerHandler : IRequestHandler<SaveClinicalMarkerCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;
    private readonly IUserService _user;

    public SaveClinicalMarkerHandler(IApplicationDbContext context, ITenantService tenant, IUserService user)
    {
        _context = context;
        _tenant = tenant;
        _user = user;
    }

    public async Task<Guid> Handle(SaveClinicalMarkerCommand request, CancellationToken ct)
    {
        var userId = _user.GetUserId();
        var professional = await _context.Professionals
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var professionalId = professional?.Id ?? userId;

        var marker = new ClinicalMarker(
            request.PatientId,
            professionalId,
            request.Target,
            request.X,
            request.Y,
            request.Type,
            request.Label,
            _tenant.GetTenantId(),
            request.AppointmentId,
            request.IntensityOrDose,
            request.Notes
        );

        _context.ClinicalMarkers.Add(marker);
        await _context.SaveChangesAsync(ct);
        return marker.Id;
    }
}

public record DeleteClinicalMarkerCommand(Guid Id) : IRequest;

public class DeleteClinicalMarkerHandler : IRequestHandler<DeleteClinicalMarkerCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteClinicalMarkerHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteClinicalMarkerCommand request, CancellationToken ct)
    {
        var marker = await _context.ClinicalMarkers.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Marcador não encontrado.");

        _context.ClinicalMarkers.Remove(marker);
        await _context.SaveChangesAsync(ct);
    }
}
