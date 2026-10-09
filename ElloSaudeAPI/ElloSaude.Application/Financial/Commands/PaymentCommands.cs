using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Financial.Commands;

// --- Command: Registrar Pagamento ---

public record RegisterPaymentCommand(
    Guid PaymentRecordId,
    decimal AmountPaid,
    int PaymentMethodId,
    string? Notes
) : IRequest<Unit>;

public class RegisterPaymentCommandValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentRecordId).NotEmpty();
        RuleFor(x => x.AmountPaid).GreaterThan(0).WithMessage("Valor pago deve ser maior que zero.");
        RuleFor(x => x.PaymentMethodId)
            .Must(id => Enum.IsDefined(typeof(PaymentMethod), id))
            .WithMessage("Forma de pagamento inválida.");
    }
}

public class RegisterPaymentCommandHandler : IRequestHandler<RegisterPaymentCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public RegisterPaymentCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Unit> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var paymentRecord = await _context.PaymentRecords
            .FirstOrDefaultAsync(p => p.Id == request.PaymentRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lançamento financeiro {request.PaymentRecordId} não encontrado.");

        paymentRecord.RegisterPayment(
            amount: request.AmountPaid,
            method: (PaymentMethod)request.PaymentMethodId,
            registeredBy: _tenantService.GetUserId(),
            notes: request.Notes
        );

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// --- Command: Marcar como Retorno Gratuito ---

public record MarkAsFreeReturnCommand(
    Guid PaymentRecordId,
    string? Reason
) : IRequest<Unit>;

public class MarkAsFreeReturnCommandValidator : AbstractValidator<MarkAsFreeReturnCommand>
{
    public MarkAsFreeReturnCommandValidator()
    {
        RuleFor(x => x.PaymentRecordId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}

public class MarkAsFreeReturnCommandHandler : IRequestHandler<MarkAsFreeReturnCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public MarkAsFreeReturnCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Unit> Handle(MarkAsFreeReturnCommand request, CancellationToken cancellationToken)
    {
        var paymentRecord = await _context.PaymentRecords
            .FirstOrDefaultAsync(p => p.Id == request.PaymentRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lançamento financeiro {request.PaymentRecordId} não encontrado.");

        var appointmentId = paymentRecord.AppointmentId
            ?? throw new InvalidOperationException("O lançamento não está associado a um agendamento.");
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken)
            ?? throw new KeyNotFoundException("Agendamento vinculado ao lançamento não encontrado.");

        if (appointment.PatientId != paymentRecord.PatientId
            || appointment.ProfessionalId != paymentRecord.ProfessionalId)
        {
            throw new InvalidOperationException("O lançamento financeiro não corresponde ao agendamento associado.");
        }

        appointment.MarkAsFreeReturn();
        paymentRecord.MarkAsFreeReturn(
            registeredBy: _tenantService.GetUserId(),
            reason: request.Reason
        );

        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public record CancelPaymentCommand(
    Guid PaymentRecordId,
    string Reason
) : IRequest<Unit>;

public class CancelPaymentCommandValidator : AbstractValidator<CancelPaymentCommand>
{
    public CancelPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentRecordId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CancelPaymentCommandHandler : IRequestHandler<CancelPaymentCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;

    public CancelPaymentCommandHandler(IApplicationDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Unit> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        var paymentRecord = await _context.PaymentRecords
            .FirstOrDefaultAsync(p => p.Id == request.PaymentRecordId, cancellationToken)
            ?? throw new KeyNotFoundException($"Lançamento financeiro {request.PaymentRecordId} não encontrado.");

        paymentRecord.Cancel(_tenantService.GetUserId(), request.Reason);
        await _context.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
