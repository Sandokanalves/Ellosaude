using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.PatientPortal;

public sealed record CancelMyAppointmentCommand(Guid AppointmentId) : IRequest;

public sealed class CancelMyAppointmentValidator : AbstractValidator<CancelMyAppointmentCommand>
{
    public CancelMyAppointmentValidator()
    {
        RuleFor(request => request.AppointmentId).NotEmpty();
    }
}

public sealed class CancelMyAppointmentHandler : IRequestHandler<CancelMyAppointmentCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;

    public CancelMyAppointmentHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IUserService userService)
    {
        _context = context;
        _tenantService = tenantService;
        _userService = userService;
    }

    public async Task Handle(CancelMyAppointmentCommand request, CancellationToken cancellationToken)
    {
        var userId = _userService.GetUserId();
        var patientId = await _context.Patients
            .Where(patient => patient.UserId == userId && patient.IsActive)
            .Select(patient => (Guid?)patient.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException("A conta não está vinculada a um paciente ativo.");

        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(
                item => item.Id == request.AppointmentId && item.PatientId == patientId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Agendamento não encontrado.");

        if (appointment.StartTime <= DateTime.UtcNow.AddHours(24)
            || appointment.Status is not (AppointmentStatus.Pendente or AppointmentStatus.Confirmado))
            throw new InvalidOperationException(
                "O cancelamento pelo portal exige pelo menos 24 horas de antecedência e um agendamento ativo.");

        appointment.Cancel("Cancelado pelo paciente no portal.");
        var financialRecord = await _context.PaymentRecords
            .FirstOrDefaultAsync(payment => payment.AppointmentId == appointment.Id, cancellationToken);
        if (financialRecord is not null
            && financialRecord.AmountPaid == 0
            && financialRecord.Status != PaymentStatus.Cancelado)
            financialRecord.Cancel(userId, "Agendamento cancelado pelo paciente.");

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        _context.AuditLogs.Add(new AuditLog(
            userId,
            user?.Email ?? string.Empty,
            _userService.GetUserRole(),
            "Cancelar agendamento pelo portal",
            nameof(Appointment),
            appointment.Id.ToString(),
            _tenantService.GetTenantId()));
        await _context.SaveChangesAsync(cancellationToken);
    }
}
