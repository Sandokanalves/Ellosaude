using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Financial.Queries;

// === DTOs de Relatório Financeiro ===

public record FinancialReportDto(
    decimal TotalReceived,
    decimal TotalPending,
    int TotalFreeReturns,
    decimal TotalRevenue,
    int PaidCount,
    int PendingCount,
    int FreeReturnCount,
    IEnumerable<PaymentByMethodDto> ByPaymentMethod,
    IEnumerable<PaymentByProfessionalDto> ByProfessional,
    IEnumerable<PaymentTransactionDto> Transactions
);

public record PaymentByMethodDto(string Method, decimal TotalAmount, int Count);
public record PaymentByProfessionalDto(Guid ProfessionalId, string ProfessionalName, decimal TotalReceived, int PaidCount);
public record PaymentTransactionDto(
    Guid Id,
    Guid? AppointmentId,
    string PatientName,
    string ProfessionalName,
    decimal ExpectedAmount,
    decimal AmountPaid,
    string Status,
    string? Method,
    DateTime? PaymentDate,
    DateTime CreatedAt
);

// === Query ===

public record GetFinancialReportQuery(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    Guid? ProfessionalId = null
) : IRequest<FinancialReportDto>;

public record GetPendingPaymentsQuery(
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<IEnumerable<PaymentTransactionDto>>;

public class GetPendingPaymentsQueryHandler : IRequestHandler<GetPendingPaymentsQuery, IEnumerable<PaymentTransactionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingPaymentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PaymentTransactionDto>> Handle(
        GetPendingPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.PaymentRecords
            .Include(payment => payment.Patient)
            .Include(payment => payment.Professional)
            .Where(payment =>
                payment.Status == PaymentStatus.Pendente
                || payment.Status == PaymentStatus.ParcialmentePago);

        if (request.StartDate.HasValue)
            query = query.Where(payment => payment.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(payment =>
                payment.CreatedAt <= request.EndDate.Value.AddDays(1).AddTicks(-1));

        var records = await query
            .OrderBy(payment => payment.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.Select(payment => new PaymentTransactionDto(
            payment.Id,
            payment.AppointmentId,
            payment.Patient?.Name ?? "—",
            payment.Professional?.Name ?? "—",
            payment.ExpectedAmount,
            payment.AmountPaid,
            payment.Status.ToString(),
            payment.Method?.ToString(),
            payment.PaymentDate,
            payment.CreatedAt
        ));
    }
}

public class GetFinancialReportQueryHandler : IRequestHandler<GetFinancialReportQuery, FinancialReportDto>
{
    private readonly IApplicationDbContext _context;

    public GetFinancialReportQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialReportDto> Handle(GetFinancialReportQuery request, CancellationToken ct)
    {
        var query = _context.PaymentRecords
            .Include(p => p.Patient)
            .Include(p => p.Professional)
            .AsQueryable();

        if (request.StartDate.HasValue)
            query = query.Where(p => p.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(p => p.CreatedAt <= request.EndDate.Value.AddDays(1).AddTicks(-1));

        if (request.ProfessionalId.HasValue)
            query = query.Where(p => p.ProfessionalId == request.ProfessionalId.Value);

        var records = await query.ToListAsync(ct);

        var totalReceived = records.Where(r => r.Status == PaymentStatus.Pago || r.Status == PaymentStatus.ParcialmentePago)
            .Sum(r => r.AmountPaid);

        var totalPending = records
            .Where(r => r.Status == PaymentStatus.Pendente || r.Status == PaymentStatus.ParcialmentePago)
            .Sum(r => r.ExpectedAmount - r.AmountPaid);

        var freeReturnCount = records.Count(r => r.Status == PaymentStatus.GratuitoRetorno);

        // Totais por forma de pagamento
        var byMethod = records
            .Where(r => r.Method.HasValue && r.AmountPaid > 0)
            .GroupBy(r => r.Method!.Value)
            .Select(g => new PaymentByMethodDto(g.Key.ToString(), g.Sum(r => r.AmountPaid), g.Count()))
            .ToList();

        // Totais por profissional
        var byProfessional = records
            .Where(r => r.Status == PaymentStatus.Pago || r.Status == PaymentStatus.ParcialmentePago)
            .GroupBy(r => new { r.ProfessionalId, r.Professional?.Name })
            .Select(g => new PaymentByProfessionalDto(g.Key.ProfessionalId, g.Key.Name ?? "—", g.Sum(r => r.AmountPaid), g.Count()))
            .ToList();

        var transactions = records.Select(r => new PaymentTransactionDto(
            r.Id,
            r.AppointmentId,
            r.Patient?.Name ?? "—",
            r.Professional?.Name ?? "—",
            r.ExpectedAmount,
            r.AmountPaid,
            r.Status.ToString(),
            r.Method?.ToString(),
            r.PaymentDate,
            r.CreatedAt
        )).ToList();

        return new FinancialReportDto(
            TotalReceived: totalReceived,
            TotalPending: totalPending,
            TotalFreeReturns: freeReturnCount,
            TotalRevenue: totalReceived + totalPending,
            PaidCount: records.Count(r => r.Status == PaymentStatus.Pago),
            PendingCount: records.Count(r =>
                r.Status == PaymentStatus.Pendente || r.Status == PaymentStatus.ParcialmentePago),
            FreeReturnCount: freeReturnCount,
            ByPaymentMethod: byMethod,
            ByProfessional: byProfessional,
            Transactions: transactions
        );
    }
}

// === Mantém a query simples antiga para backward compatibility ===

public record GetFinancialSummaryQuery(DateTime? StartDate = null, DateTime? EndDate = null) : IRequest<FinancialSummaryDto>;

public record FinancialSummaryDto(
    decimal TotalReceived,
    decimal TotalPending,
    decimal TotalRevenue,
    int CompletedCount,
    int PendingCount
);

public class GetFinancialSummaryHandler : IRequestHandler<GetFinancialSummaryQuery, FinancialSummaryDto>
{
    private readonly IApplicationDbContext _context;

    public GetFinancialSummaryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FinancialSummaryDto> Handle(GetFinancialSummaryQuery request, CancellationToken ct)
    {
        var query = _context.PaymentRecords.AsQueryable();

        if (request.StartDate.HasValue)
            query = query.Where(p => p.CreatedAt >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(p => p.CreatedAt <= request.EndDate.Value.AddDays(1).AddTicks(-1));

        var records = await query.ToListAsync(ct);

        var totalReceived = records.Where(r => r.Status == PaymentStatus.Pago || r.Status == PaymentStatus.ParcialmentePago).Sum(r => r.AmountPaid);
        var totalPending = records
            .Where(r => r.Status == PaymentStatus.Pendente || r.Status == PaymentStatus.ParcialmentePago)
            .Sum(r => r.ExpectedAmount - r.AmountPaid);

        return new FinancialSummaryDto(
            TotalReceived: totalReceived,
            TotalPending: totalPending,
            TotalRevenue: totalReceived + totalPending,
            CompletedCount: records.Count(r => r.Status == PaymentStatus.Pago),
            PendingCount: records.Count(r =>
                r.Status == PaymentStatus.Pendente || r.Status == PaymentStatus.ParcialmentePago)
        );
    }
}
