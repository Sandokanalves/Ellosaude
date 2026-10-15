using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.MedicalDocuments.Queries;

public record GetMedicalDocumentsByPatientQuery(Guid PatientId) : IRequest<IEnumerable<MedicalDocumentDto>>;

public class GetMedicalDocumentsByPatientHandler : IRequestHandler<GetMedicalDocumentsByPatientQuery, IEnumerable<MedicalDocumentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMedicalDocumentsByPatientHandler(IApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<MedicalDocumentDto>> Handle(GetMedicalDocumentsByPatientQuery request, CancellationToken ct)
    {
        var docs = await _context.MedicalDocuments
            .Where(d => d.PatientId == request.PatientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        var professionalIds = docs.Select(d => d.ProfessionalId).Distinct().ToList();
        var professionals = await _context.Professionals
            .Where(p => professionalIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        return docs.Select(d =>
        {
            professionals.TryGetValue(d.ProfessionalId, out var prof);
            return new MedicalDocumentDto(
                d.Id,
                d.PatientId,
                d.ProfessionalId,
                d.AppointmentId,
                d.DocumentType,
                d.Title,
                d.Content,
                d.Cid,
                d.DaysOff,
                d.CreatedAt,
                prof?.Name,
                prof?.Crm,
                prof?.CrmState,
                prof?.CouncilType
            );
        });
    }
}

public record GetMedicalDocumentByIdQuery(Guid Id) : IRequest<MedicalDocumentDto>;

public class GetMedicalDocumentByIdHandler : IRequestHandler<GetMedicalDocumentByIdQuery, MedicalDocumentDto>
{
    private readonly IApplicationDbContext _context;

    public GetMedicalDocumentByIdHandler(IApplicationDbContext context) => _context = context;

    public async Task<MedicalDocumentDto> Handle(GetMedicalDocumentByIdQuery request, CancellationToken ct)
    {
        var d = await _context.MedicalDocuments.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Documento não encontrado.");

        var prof = await _context.Professionals.FindAsync(new object[] { d.ProfessionalId }, ct);

        return new MedicalDocumentDto(
            d.Id,
            d.PatientId,
            d.ProfessionalId,
            d.AppointmentId,
            d.DocumentType,
            d.Title,
            d.Content,
            d.Cid,
            d.DaysOff,
            d.CreatedAt,
            prof?.Name,
            prof?.Crm,
            prof?.CrmState,
            prof?.CouncilType
        );
    }
}
