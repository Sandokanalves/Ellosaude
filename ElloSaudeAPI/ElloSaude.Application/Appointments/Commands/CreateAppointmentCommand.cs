using MediatR;
using FluentValidation;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Appointments.Commands;

public record CreateAppointmentCommand(
    Guid PatientId,
    Guid ProfessionalId,
    DateTime Start,
    DateTime End,
    int TypeId,
    decimal? Price = null,
    bool IsFreeReturn = false,
    string? Observations = null
) : IRequest<Guid>;

public class CreateAppointmentValidator : AbstractValidator<CreateAppointmentCommand>
{
    public CreateAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Paciente é obrigatório.");
        RuleFor(x => x.ProfessionalId).NotEmpty().WithMessage("Profissional é obrigatório.");
        RuleFor(x => x.Start).GreaterThan(DateTime.UtcNow).WithMessage("Data do agendamento deve ser futura.");
        RuleFor(x => x.End).GreaterThan(x => x.Start).WithMessage("Horário de término deve ser posterior ao início.");
        RuleFor(x => x.End.Date).Equal(x => x.Start.Date).WithMessage("O agendamento deve iniciar e terminar no mesmo dia.");
        RuleFor(x => x.TypeId)
            .Must(id => Enum.IsDefined(typeof(AppointmentType), id))
            .WithMessage("Tipo de consulta inválido.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0).When(x => x.Price.HasValue).WithMessage("Preço não pode ser negativo.");
        RuleFor(x => x.TypeId)
            .Equal((int)AppointmentType.Retorno)
            .When(x => x.IsFreeReturn)
            .WithMessage("Somente consultas de retorno podem ser gratuitas.");
    }
}

public class CreateAppointmentHandler : IRequestHandler<CreateAppointmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;

    public CreateAppointmentHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IUserService userService)
    {
        _context = context;
        _tenantService = tenantService;
        _userService = userService;
    }

    public async Task<Guid> Handle(CreateAppointmentCommand request, CancellationToken ct)
    {
        var tenantId = _tenantService.GetTenantId();

        if (_userService.GetUserRole() == "Paciente")
        {
            var userId = _userService.GetUserId();
            var linkedPatientId = await _context.Patients
                .Where(patient => patient.UserId == userId && patient.IsActive)
                .Select(patient => (Guid?)patient.Id)
                .FirstOrDefaultAsync(ct)
                ?? throw new UnauthorizedAccessException("A conta não está vinculada a um paciente ativo.");

            if (request.PatientId != linkedPatientId
                || request.IsFreeReturn
                || request.TypeId != (int)AppointmentType.Consulta
                || request.Price.HasValue)
                throw new UnauthorizedAccessException("O portal só permite agendar consultas para o próprio paciente.");
        }

        // 1. Valida que o paciente existe e pertence ao tenant
        var patientExists = await _context.Patients.AnyAsync(p => p.Id == request.PatientId, ct);
        if (!patientExists)
            throw new KeyNotFoundException($"Paciente {request.PatientId} não encontrado.");

        // 2. Valida que o profissional existe, está ativo e pertence ao tenant
        var professional = await _context.Professionals
            .FirstOrDefaultAsync(p => p.Id == request.ProfessionalId && p.IsActive, ct)
            ?? throw new KeyNotFoundException($"Profissional {request.ProfessionalId} não encontrado ou inativo.");

        // 3. Valida disponibilidade semanal do profissional
        var dayOfWeek = request.Start.DayOfWeek;
        var availability = await _context.DoctorAvailabilities
            .FirstOrDefaultAsync(a => a.ProfessionalId == request.ProfessionalId && a.DayOfWeek == dayOfWeek, ct);

        if (availability == null)
            throw new InvalidOperationException("O profissional não possui disponibilidade configurada para este dia.");

        var requestStartTime = request.Start.TimeOfDay;
        var requestEndTime = request.End.TimeOfDay;

        if (requestStartTime < availability.StartTime || requestEndTime > availability.EndTime)
            throw new InvalidOperationException(
                $"Horário fora da disponibilidade do profissional. Atende das {availability.StartTime:hh\\:mm} às {availability.EndTime:hh\\:mm}.");

        var slotDuration = TimeSpan.FromMinutes(availability.SlotDurationMinutes);
        if (request.End - request.Start != slotDuration
            || (requestStartTime - availability.StartTime).Ticks % slotDuration.Ticks != 0)
            throw new InvalidOperationException("O agendamento deve corresponder a um horário disponível completo.");

        if (availability.BreakStartTime.HasValue && availability.BreakEndTime.HasValue
            && requestStartTime < availability.BreakEndTime.Value
            && requestEndTime > availability.BreakStartTime.Value)
        {
            throw new InvalidOperationException(
                $"Agendamento conflita com o intervalo do profissional ({availability.BreakStartTime:hh\\:mm} - {availability.BreakEndTime:hh\\:mm}).");
        }

        // 4. Verifica bloqueios de agenda (férias, folgas, etc.)
        var isBlocked = await _context.ScheduleBlocks.AnyAsync(b =>
            b.ProfessionalId == request.ProfessionalId
            && b.IsActive
            && b.StartDateTime < request.End
            && b.EndDateTime > request.Start, ct);

        if (isBlocked)
            throw new InvalidOperationException("O profissional está com a agenda bloqueada nesta data.");

        // 5. Verifica conflito de horário do profissional (double-booking)
        var hasConflict = await _context.Appointments.AnyAsync(a =>
            a.ProfessionalId == request.ProfessionalId
            && a.Status != AppointmentStatus.Cancelado
            && a.StartTime < request.End
            && a.EndTime > request.Start, ct);

        if (hasConflict)
            throw new InvalidOperationException("Conflito de horário: o profissional já possui um agendamento neste período.");

        // 6. Verifica conflito do paciente no mesmo horário
        var patientHasConflict = await _context.Appointments.AnyAsync(a =>
            a.PatientId == request.PatientId
            && a.Status != AppointmentStatus.Cancelado
            && a.StartTime < request.End
            && a.EndTime > request.Start, ct);

        if (patientHasConflict)
            throw new InvalidOperationException("O paciente já possui um agendamento neste horário.");

        // 7. Define o preço: se não informado, usa o preço padrão do profissional
        var price = request.Price ?? professional.ConsultationPrice;
        if (request.IsFreeReturn) price = 0;

        // 8. Cria o agendamento
        var appointment = new Appointment(
            start: request.Start,
            end: request.End,
            patientId: request.PatientId,
            professionalId: request.ProfessionalId,
            type: (AppointmentType)request.TypeId,
            tenantId: tenantId,
            price: price,
            isFreeReturn: request.IsFreeReturn,
            observations: request.Observations
        );

        _context.Appointments.Add(appointment);

        // 9. Cria o lançamento financeiro pendente
        var paymentRecord = new PaymentRecord(
            appointmentId: appointment.Id,
            patientId: request.PatientId,
            professionalId: request.ProfessionalId,
            expectedAmount: price,
            tenantId: tenantId,
            registeredByUserId: _tenantService.GetUserId(),
            isFreeReturn: request.IsFreeReturn,
            notes: request.IsFreeReturn ? "Consulta de retorno - sem cobrança" : null
        );

        _context.PaymentRecords.Add(paymentRecord);

        await _context.SaveChangesAsync(ct);
        return appointment.Id;
    }
}