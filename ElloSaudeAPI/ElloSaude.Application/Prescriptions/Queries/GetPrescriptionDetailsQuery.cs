using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Prescriptions.Queries;

public record PrescriptionItemDetailsDto(
    string MedicationName,
    string Dosage,
    string Frequency,
    string Duration,
    string Route,
    string? Instructions
);

public record PrescriptionDetailsDto(
    Guid Id,
    string PatientName,
    DateTime PatientBirthDate,
    string ProfessionalName,
    string Crm,
    string CrmState,
    string Specialty,
    DateTime IssueDate,
    string? Notes,
    IReadOnlyCollection<PrescriptionItemDetailsDto> Items
);

public record GetPrescriptionDetailsQuery(Guid Id) : IRequest<PrescriptionDetailsDto>;

public class GetPrescriptionDetailsQueryHandler
    : IRequestHandler<GetPrescriptionDetailsQuery, PrescriptionDetailsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;
    private readonly ITenantService _tenantService;

    public GetPrescriptionDetailsQueryHandler(
        IApplicationDbContext context,
        IUserService userService,
        ITenantService tenantService)
    {
        _context = context;
        _userService = userService;
        _tenantService = tenantService;
    }

    public async Task<PrescriptionDetailsDto> Handle(
        GetPrescriptionDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var prescription = await _context.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Receita não encontrada.");

        if (prescription.Status != ElloSaude.Domain.Enums.PrescriptionStatus.Emitida)
            throw new KeyNotFoundException("Receita não encontrada.");

        var userId = _userService.GetUserId();
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == prescription.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException("Receita não encontrada.");

        var professional = await _context.Professionals
            .FirstOrDefaultAsync(
                p => p.Id == prescription.DoctorId && p.UserId == userId,
                cancellationToken);

        if (professional == null && patient.UserId != userId)
            throw new KeyNotFoundException("Receita não encontrada.");

        professional ??= await _context.Professionals
            .FirstOrDefaultAsync(p => p.Id == prescription.DoctorId, cancellationToken)
            ?? throw new KeyNotFoundException("Receita não encontrada.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        _context.AuditLogs.Add(new AuditLog(
            userId: userId,
            userEmail: user?.Email ?? string.Empty,
            userRole: _userService.GetUserRole(),
            action: "Acessar receita",
            entityName: nameof(Prescription),
            entityId: prescription.Id.ToString(),
            tenantId: _tenantService.GetTenantId()
        ));
        await _context.SaveChangesAsync(cancellationToken);

        return new PrescriptionDetailsDto(
            prescription.Id,
            patient.Name,
            patient.BirthDate,
            professional.Name,
            professional.Crm,
            professional.CrmState,
            professional.Specialty,
            prescription.IssueDate,
            prescription.Notes,
            prescription.Items
                .Select(item => new PrescriptionItemDetailsDto(
                    item.MedicineName,
                    item.Dosage,
                    item.Frequency,
                    item.Duration,
                    item.Route,
                    item.Instructions))
                .ToArray()
        );
    }
}
