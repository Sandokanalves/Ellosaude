using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Financial.Commands;

public record CreateClinicExpenseCommand(
    string Description,
    string Category,
    decimal Amount,
    DateTime DueDate,
    bool IsPaid = false,
    DateTime? PaidDate = null,
    string? PaymentMethod = null,
    string? Notes = null) : IRequest<Guid>;

public class CreateClinicExpenseValidator : AbstractValidator<CreateClinicExpenseCommand>
{
    public CreateClinicExpenseValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
    }
}

public class CreateClinicExpenseHandler : IRequestHandler<CreateClinicExpenseCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;

    public CreateClinicExpenseHandler(IApplicationDbContext context, ITenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    public async Task<Guid> Handle(CreateClinicExpenseCommand request, CancellationToken ct)
    {
        var expense = new ClinicExpense(
            request.Description,
            request.Category,
            request.Amount,
            request.DueDate,
            _tenant.GetTenantId(),
            request.IsPaid,
            request.PaidDate,
            request.PaymentMethod,
            request.Notes
        );

        _context.ClinicExpenses.Add(expense);
        await _context.SaveChangesAsync(ct);
        return expense.Id;
    }
}

public record PayClinicExpenseCommand(Guid Id, DateTime? PaidDate = null, string? PaymentMethod = null) : IRequest;

public class PayClinicExpenseHandler : IRequestHandler<PayClinicExpenseCommand>
{
    private readonly IApplicationDbContext _context;

    public PayClinicExpenseHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(PayClinicExpenseCommand request, CancellationToken ct)
    {
        var expense = await _context.ClinicExpenses.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Despesa não encontrada.");

        expense.MarkAsPaid(request.PaidDate, request.PaymentMethod);
        await _context.SaveChangesAsync(ct);
    }
}

public record DeleteClinicExpenseCommand(Guid Id) : IRequest;

public class DeleteClinicExpenseHandler : IRequestHandler<DeleteClinicExpenseCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteClinicExpenseHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteClinicExpenseCommand request, CancellationToken ct)
    {
        var expense = await _context.ClinicExpenses.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Despesa não encontrada.");

        _context.ClinicExpenses.Remove(expense);
        await _context.SaveChangesAsync(ct);
    }
}
