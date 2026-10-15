using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using ElloSaude.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Financial.Queries;

public record GetDreReportQuery(DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<DreReportDto>;

public class GetDreReportHandler : IRequestHandler<GetDreReportQuery, DreReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetDreReportHandler(IApplicationDbContext context) => _context = context;

    public async Task<DreReportDto> Handle(GetDreReportQuery request, CancellationToken ct)
    {
        var start = request.StartDate ?? new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var end = request.EndDate ?? start.AddMonths(1).AddTicks(-1);

        // 1. Receitas
        var payments = await _context.PaymentRecords
            .Where(p => p.Status == PaymentStatus.Pago && p.PaymentDate >= start && p.PaymentDate <= end)
            .ToListAsync(ct);

        var grossRevenue = payments.Sum(p => p.AmountPaid);

        var revenueByMethod = payments
            .GroupBy(p => p.Method?.ToString() ?? "Outro")
            .Select(g => new DreCategoryAmountDto(g.Key, g.Sum(x => x.AmountPaid)))
            .ToList();

        // 2. Comissões dos Profissionais
        var professionals = await _context.Professionals.ToListAsync(ct);
        var profDict = professionals.ToDictionary(p => p.Id, p => p.CommissionPercentage);

        decimal totalCommissions = 0;
        foreach (var p in payments)
        {
            var commissionPct = profDict.TryGetValue(p.ProfessionalId, out var pct) ? pct : 50.0m;
            totalCommissions += p.AmountPaid * (commissionPct / 100m);
        }

        // 3. Despesas da Clínica
        var expenses = await _context.ClinicExpenses
            .Where(e => (e.IsPaid ? e.PaidDate : e.DueDate) >= start && (e.IsPaid ? e.PaidDate : e.DueDate) <= end)
            .ToListAsync(ct);

        var totalExpenses = expenses.Sum(e => e.Amount);

        var expensesByCategory = expenses
            .GroupBy(e => e.Category)
            .Select(g => new DreCategoryAmountDto(g.Key, g.Sum(x => x.Amount)))
            .ToList();

        // 4. Lucro Líquido
        var netProfit = grossRevenue - totalCommissions - totalExpenses;
        var profitMargin = grossRevenue > 0 ? (netProfit / grossRevenue) * 100m : 0;

        return new DreReportDto(
            start,
            end,
            grossRevenue,
            totalCommissions,
            totalExpenses,
            netProfit,
            Math.Round(profitMargin, 1),
            revenueByMethod,
            expensesByCategory
        );
    }
}
