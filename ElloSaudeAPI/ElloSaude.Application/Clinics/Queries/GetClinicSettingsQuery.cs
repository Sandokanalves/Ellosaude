using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Clinics.Queries;

public record GetClinicSettingsQuery() : IRequest<ClinicSettingsDto>;

public class GetClinicSettingsHandler : IRequestHandler<GetClinicSettingsQuery, ClinicSettingsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;

    public GetClinicSettingsHandler(IApplicationDbContext context, ITenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    public async Task<ClinicSettingsDto> Handle(GetClinicSettingsQuery request, CancellationToken ct)
    {
        var tenantId = _tenant.GetTenantId();
        var clinic = await _context.Clinics
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct)
            ?? throw new KeyNotFoundException("Configurações da clínica não encontradas.");

        return new ClinicSettingsDto(
            clinic.Id,
            clinic.Name,
            clinic.Cnpj,
            clinic.NameFantasy,
            clinic.Phone,
            clinic.Email,
            clinic.Address,
            clinic.City,
            clinic.State,
            clinic.LogoUrl,
            clinic.MaxReturnDays,
            clinic.DefaultCommissionPercentage,
            clinic.TenantId ?? string.Empty
        );
    }
}
