using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.ClinicalMarkers.Queries;

public record GetClinicalMarkersQuery(Guid PatientId, string? Target = null) : IRequest<IEnumerable<ClinicalMarkerDto>>;

public class GetClinicalMarkersHandler : IRequestHandler<GetClinicalMarkersQuery, IEnumerable<ClinicalMarkerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetClinicalMarkersHandler(IApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<ClinicalMarkerDto>> Handle(GetClinicalMarkersQuery request, CancellationToken ct)
    {
        var query = _context.ClinicalMarkers
            .Where(m => m.PatientId == request.PatientId);

        if (!string.IsNullOrWhiteSpace(request.Target))
            query = query.Where(m => m.Target == request.Target);

        var list = await query.OrderByDescending(m => m.CreatedAt).ToListAsync(ct);

        return list.Select(m => new ClinicalMarkerDto(
            m.Id,
            m.PatientId,
            m.AppointmentId,
            m.ProfessionalId,
            m.Target,
            m.X,
            m.Y,
            m.Type,
            m.Label,
            m.IntensityOrDose,
            m.Notes,
            m.CreatedAt
        ));
    }
}
