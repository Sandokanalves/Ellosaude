using ElloSaude.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Professionals.Queries;

public record GetProfessionalsQuery : IRequest<IEnumerable<ProfessionalDto>>;

public record ProfessionalDto(
    Guid Id,
    string Name,
    string Crm,
    string CrmState,
    string Specialty,
    string? Phone,
    string? Email,
    decimal ConsultationPrice,
    bool IsActive
);

public class GetProfessionalsQueryHandler : IRequestHandler<GetProfessionalsQuery, IEnumerable<ProfessionalDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProfessionalsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProfessionalDto>> Handle(GetProfessionalsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Professionals
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new ProfessionalDto(
                p.Id,
                p.Name,
                p.Crm,
                p.CrmState,
                p.Specialty,
                p.Phone,
                p.Email,
                p.ConsultationPrice,
                p.IsActive
            ))
            .ToListAsync(cancellationToken);
    }
}
