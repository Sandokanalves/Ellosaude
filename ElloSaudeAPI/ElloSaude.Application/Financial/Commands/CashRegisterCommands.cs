using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Financial.Commands;

public record OpenCashRegisterCommand(decimal OpeningBalance, string? Notes = null) : IRequest<Guid>;

public class OpenCashRegisterValidator : AbstractValidator<OpenCashRegisterCommand>
{
    public OpenCashRegisterValidator()
    {
        RuleFor(x => x.OpeningBalance).GreaterThanOrEqualTo(0);
    }
}

public class OpenCashRegisterHandler : IRequestHandler<OpenCashRegisterCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;
    private readonly IUserService _user;

    public OpenCashRegisterHandler(IApplicationDbContext context, ITenantService tenant, IUserService user)
    {
        _context = context;
        _tenant = tenant;
        _user = user;
    }

    public async Task<Guid> Handle(OpenCashRegisterCommand request, CancellationToken ct)
    {
        var hasOpenSession = await _context.CashRegisterSessions
            .AnyAsync(s => s.Status == "Aberto", ct);

        if (hasOpenSession)
            throw new InvalidOperationException("Já existe uma sessão de caixa aberta. Feche a sessão anterior antes de abrir uma nova.");

        var userId = _user.GetUserId();
        var session = new CashRegisterSession(
            request.OpeningBalance,
            userId,
            _tenant.GetTenantId(),
            request.Notes
        );

        _context.CashRegisterSessions.Add(session);
        await _context.SaveChangesAsync(ct);
        return session.Id;
    }
}

public record CloseCashRegisterCommand(decimal ClosingBalance, string? Notes = null) : IRequest;

public class CloseCashRegisterValidator : AbstractValidator<CloseCashRegisterCommand>
{
    public CloseCashRegisterValidator()
    {
        RuleFor(x => x.ClosingBalance).GreaterThanOrEqualTo(0);
    }
}

public class CloseCashRegisterHandler : IRequestHandler<CloseCashRegisterCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _user;

    public CloseCashRegisterHandler(IApplicationDbContext context, IUserService user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(CloseCashRegisterCommand request, CancellationToken ct)
    {
        var session = await _context.CashRegisterSessions
            .Where(s => s.Status == "Aberto")
            .OrderByDescending(s => s.OpenedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Não há nenhuma sessão de caixa aberta no momento.");

        var userId = _user.GetUserId();
        session.Close(request.ClosingBalance, userId, request.Notes);
        await _context.SaveChangesAsync(ct);
    }
}
