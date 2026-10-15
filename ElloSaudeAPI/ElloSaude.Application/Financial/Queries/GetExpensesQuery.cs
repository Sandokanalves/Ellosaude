using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Financial.Queries;

public record GetExpensesQuery(DateTime? StartDate = null, DateTime? EndDate = null, string? Category = null) : IRequest<IEnumerable<ClinicExpenseDto>>;

public class GetExpensesHandler : IRequestHandler<GetExpensesQuery, IEnumerable<ClinicExpenseDto>>
{
    private readonly IApplicationDbContext _context;

    public GetExpensesHandler(IApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<ClinicExpenseDto>> Handle(GetExpensesQuery request, CancellationToken ct)
    {
        var query = _context.ClinicExpenses.AsNoTracking();

        if (request.StartDate.HasValue)
            query = query.Where(e => e.DueDate >= request.StartDate.Value);

        if (request.EndDate.HasValue)
            query = query.Where(e => e.DueDate <= request.EndDate.Value);

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(e => e.Category == request.Category);

        var list = await query.OrderByDescending(e => e.DueDate).ToListAsync(ct);

        return list.Select(e => new ClinicExpenseDto(
            e.Id,
            e.Description,
            e.Category,
            e.Amount,
            e.DueDate,
            e.PaidDate,
            e.IsPaid,
            e.PaymentMethod,
            e.Notes,
            e.CreatedAt
        ));
    }
}
