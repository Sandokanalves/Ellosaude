using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using ElloSaude.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Financial.Queries;

public record GetFinancialSummaryQuery(DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<FinancialSummaryDto>;

public class GetFinancialSummaryHandler : IRequestHandler<GetFinancialSummaryQuery, FinancialSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetFinancialSummaryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialSummaryDto> Handle(GetFinancialSummaryQuery request, CancellationToken ct)
    {
        var query = _context.Appointments.AsQueryable();

        if (request.StartDate.HasValue)
        {
            query = query.Where(a => a.StartTime >= request.StartDate.Value);
        }

        if (request.EndDate.HasValue)
        {
            query = query.Where(a => a.EndTime <= request.EndDate.Value);
        }

        var appointments = await query.ToListAsync(ct);

        const decimal consultationPrice = 150.00m; // Preço padrão da consulta para cálculo de faturamento

        var completed = appointments.Count(a => a.Status == AppointmentStatus.Realizado);
        var pending = appointments.Count(a => a.Status == AppointmentStatus.Pendente || a.Status == AppointmentStatus.Confirmado);

        var totalReceived = completed * consultationPrice;
        var totalPending = pending * consultationPrice;
        var totalRevenue = totalReceived + totalPending;

        return new FinancialSummaryDto(
            totalReceived,
            totalPending,
            totalRevenue,
            completed,
            pending
        );
    }
}
