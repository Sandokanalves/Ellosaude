using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using ElloSaude.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Financial.Queries;

public record GetCurrentCashRegisterQuery() : IRequest<CashRegisterSessionDto?>;

public class GetCurrentCashRegisterHandler : IRequestHandler<GetCurrentCashRegisterQuery, CashRegisterSessionDto?>
{
    private readonly IApplicationDbContext _context;

    public GetCurrentCashRegisterHandler(IApplicationDbContext context) => _context = context;

    public async Task<CashRegisterSessionDto?> Handle(GetCurrentCashRegisterQuery request, CancellationToken ct)
    {
        var session = await _context.CashRegisterSessions
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(ct);

        if (session == null)
            return null;

        // Calculate payments received today if session is open
        decimal totalReceived = 0;
        if (session.Status == "Aberto")
        {
            totalReceived = await _context.PaymentRecords
                .Where(p => p.Status == PaymentStatus.Pago && p.PaymentDate >= session.OpenedAt)
                .SumAsync(p => p.AmountPaid, ct);
        }

        var currentBalance = session.OpeningBalance + totalReceived;

        return new CashRegisterSessionDto(
            session.Id,
            session.OpenedAt,
            session.ClosedAt,
            session.OpeningBalance,
            session.ClosingBalance,
            session.Status,
            session.OpenedByUserId,
            session.ClosedByUserId,
            session.Notes,
            totalReceived,
            currentBalance
        );
    }
}
