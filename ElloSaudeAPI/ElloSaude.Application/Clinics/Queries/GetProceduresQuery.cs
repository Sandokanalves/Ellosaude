using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Clinics.Queries;

public record GetProceduresQuery(string? Specialty = null, bool ActiveOnly = false) : IRequest<IEnumerable<ProcedureDto>>;

public class GetProceduresHandler : IRequestHandler<GetProceduresQuery, IEnumerable<ProcedureDto>>
{
    private readonly IApplicationDbContext _context;

    public GetProceduresHandler(IApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<ProcedureDto>> Handle(GetProceduresQuery request, CancellationToken ct)
    {
        var query = _context.Procedures.AsNoTracking();

        if (request.ActiveOnly)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Specialty))
            query = query.Where(p => p.Specialty == request.Specialty);

        var list = await query.OrderBy(p => p.Specialty).ThenBy(p => p.Name).ToListAsync(ct);

        return list.Select(p => new ProcedureDto(
            p.Id,
            p.Name,
            p.Specialty,
            p.Price,
            p.DurationMinutes,
            p.ColorHex,
            p.IsActive
        ));
    }
}
