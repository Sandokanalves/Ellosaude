using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Clinics.Commands;

public record UpdateClinicSettingsCommand(
    string Name,
    string Cnpj,
    string NameFantasy,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? State,
    string? LogoUrl,
    int MaxReturnDays,
    decimal DefaultCommissionPercentage) : IRequest;

public class UpdateClinicSettingsValidator : AbstractValidator<UpdateClinicSettingsCommand>
{
    public UpdateClinicSettingsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cnpj).NotEmpty().MaximumLength(30);
        RuleFor(x => x.MaxReturnDays).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DefaultCommissionPercentage).InclusiveBetween(0, 100);
    }
}

public class UpdateClinicSettingsHandler : IRequestHandler<UpdateClinicSettingsCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;

    public UpdateClinicSettingsHandler(IApplicationDbContext context, ITenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    public async Task Handle(UpdateClinicSettingsCommand request, CancellationToken ct)
    {
        var tenantId = _tenant.GetTenantId();
        var clinic = await _context.Clinics
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Clínica não encontrada.");

        clinic.UpdateSettings(
            request.Name,
            request.Cnpj,
            request.NameFantasy,
            request.Phone,
            request.Email,
            request.Address,
            request.City,
            request.State,
            request.LogoUrl,
            request.MaxReturnDays,
            request.DefaultCommissionPercentage
        );

        await _context.SaveChangesAsync(ct);
    }
}
