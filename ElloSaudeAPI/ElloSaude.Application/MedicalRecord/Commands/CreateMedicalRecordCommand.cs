using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.MedicalRecords.Commands;

public record CreateMedicalRecordCommand(
    Guid PatientId,
    Guid AppointmentId,
    string Description,
    string Diagnosis,
    string? TreatmentPlan = null) : IRequest<Guid>;

public class CreateMedicalRecordValidator : AbstractValidator<CreateMedicalRecordCommand>
{
    public CreateMedicalRecordValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.AppointmentId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Diagnosis).NotEmpty();
    }
}

public class CreateMedicalRecordHandler : IRequestHandler<CreateMedicalRecordCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly ITenantService _tenant;
    private readonly IUserService _user;

    public CreateMedicalRecordHandler(IUnitOfWork uow, ITenantService tenant, IUserService user)
    {
        _uow = uow;
        _tenant = tenant;
        _user = user;
    }

    public async Task<Guid> Handle(CreateMedicalRecordCommand request, CancellationToken ct)
    {
        var userId = _user.GetUserId();
        if (userId == Guid.Empty)
            throw new UnauthorizedAccessException("Usuário autenticado inválido.");

        var patient = await _uow.Patients.GetByIdAsync(request.PatientId, ct)
            ?? throw new KeyNotFoundException($"Paciente {request.PatientId} não encontrado.");

        var appointment = await _uow.Appointments.GetByIdAsync(request.AppointmentId, ct)
            ?? throw new KeyNotFoundException("Agendamento do atendimento não encontrado.");
        if (appointment.PatientId != patient.Id || appointment.Status != ElloSaude.Domain.Enums.AppointmentStatus.Realizado)
            throw new InvalidOperationException("O prontuário deve estar vinculado a uma consulta realizada do paciente informado.");

        var professional = await _uow.Professionals.GetByIdAsync(appointment.ProfessionalId, ct);
        if (professional == null || professional.UserId != userId || !professional.IsActive)
            throw new UnauthorizedAccessException("Somente o profissional responsável pela consulta pode registrar o prontuário.");

        var record = new MedicalRecord(
            patient.Id,
            userId,
            request.Description,
            request.Diagnosis,
            _tenant.GetTenantId(),
            appointment.Id,
            request.TreatmentPlan
        );
        record.Sign(userId);

        await _uow.MedicalRecords.AddAsync(record, ct);
        var currentUser = await _uow.Users.GetByIdAsync(userId, ct);
        await _uow.AuditLogs.AddAsync(new AuditLog(
            userId,
            currentUser?.Email ?? string.Empty,
            _user.GetUserRole(),
            "Criar prontuário",
            nameof(MedicalRecord),
            record.Id.ToString(),
            _tenant.GetTenantId()
        ), ct);
        await _uow.CompleteAsync(ct);
        return record.Id;
    }
}