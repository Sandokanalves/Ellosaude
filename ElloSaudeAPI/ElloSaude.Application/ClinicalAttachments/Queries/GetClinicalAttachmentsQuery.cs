using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.ClinicalAttachments.Queries;

public record GetClinicalAttachmentsQuery(Guid PatientId, string? Category = null) : IRequest<IEnumerable<ClinicalAttachmentDto>>;

public class GetClinicalAttachmentsHandler : IRequestHandler<GetClinicalAttachmentsQuery, IEnumerable<ClinicalAttachmentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetClinicalAttachmentsHandler(IApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<ClinicalAttachmentDto>> Handle(GetClinicalAttachmentsQuery request, CancellationToken ct)
    {
        var query = _context.ClinicalAttachments
            .Where(a => a.PatientId == request.PatientId);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(a => a.Category == request.Category);

        var list = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(ct);

        return list.Select(a => new ClinicalAttachmentDto(
            a.Id,
            a.PatientId,
            a.ProfessionalId,
            a.AppointmentId,
            a.Title,
            a.Category,
            a.ContentType,
            a.FileUrl,
            a.Notes,
            a.CreatedAt
        ));
    }
}
