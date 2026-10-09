using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Prescriptions.Commands;

public record PrescriptionItemInput(
    string MedicationName,
    string Dosage,
    string Frequency,
    string Duration,
    string Route,
    string? Instructions
);

public record CreatePrescriptionCommand(
    Guid AppointmentId,
    Guid ProfessionalId,
    Guid PatientId,
    string? GeneralNotes,
    IEnumerable<PrescriptionItemInput> Items
) : IRequest<Guid>;

public class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator()
    {
        RuleFor(x => x.AppointmentId).NotEmpty().WithMessage("Agendamento é obrigatório.");
        RuleFor(x => x.ProfessionalId).NotEmpty().WithMessage("Profissional é obrigatório.");
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Paciente é obrigatório.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("A receita deve ter ao menos um medicamento.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.MedicationName).NotEmpty().MaximumLength(200).WithMessage("Nome do medicamento é obrigatório.");
            item.RuleFor(i => i.Dosage).NotEmpty().MaximumLength(200).WithMessage("Dosagem é obrigatória.");
            item.RuleFor(i => i.Frequency).NotEmpty().MaximumLength(200).WithMessage("Frequência é obrigatória.");
            item.RuleFor(i => i.Duration).NotEmpty().MaximumLength(100).WithMessage("Duração é obrigatória.");
            item.RuleFor(i => i.Route).NotEmpty().MaximumLength(100).WithMessage("Via de administração é obrigatória.");
        });
    }
}

public class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;

    public CreatePrescriptionCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IUserService userService)
    {
        _context = context;
        _tenantService = tenantService;
        _userService = userService;
    }

    public async Task<Guid> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantService.GetTenantId();

        // Valida que a consulta existe e pertence ao tenant
        var appointment = await _context.Appointments
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Agendamento {request.AppointmentId} não encontrado.");

        if (appointment.Status != AppointmentStatus.Realizado)
            throw new InvalidOperationException("Só é possível emitir receita após a realização da consulta.");

        // Valida que o profissional é o responsável pelo agendamento
        if (appointment.ProfessionalId != request.ProfessionalId)
            throw new UnauthorizedAccessException("Apenas o profissional responsável pela consulta pode emitir receitas.");

        var userId = _userService.GetUserId();
        var professional = await _context.Professionals
            .FirstOrDefaultAsync(
                p => p.Id == request.ProfessionalId && p.UserId == userId && p.IsActive,
                cancellationToken)
            ?? throw new UnauthorizedAccessException("O profissional autenticado não está vinculado ao profissional da consulta.");

        // Valida que o paciente corresponde ao agendamento
        if (appointment.PatientId != request.PatientId)
            throw new InvalidOperationException("O paciente informado não corresponde ao agendamento.");

        // Cria a receita usando o construtor da entidade real
        var prescription = new Prescription(
            patientId: request.PatientId,
            doctorId: request.ProfessionalId,
            tenantId: tenantId,
            appointmentId: request.AppointmentId,
            notes: request.GeneralNotes
        );

        // Adiciona os medicamentos via método de domínio
        foreach (var itemInput in request.Items)
        {
            prescription.AddItem(
                medicineName: itemInput.MedicationName,
                dosage: itemInput.Dosage,
                frequency: itemInput.Frequency,
                duration: itemInput.Duration,
                route: itemInput.Route,
                instructions: itemInput.Instructions
            );
        }

        // Finaliza a receita (muda status para Emitida)
        var canonicalContent = string.Join('|',
            prescription.Id,
            prescription.PatientId,
            professional.Id,
            prescription.IssueDate.ToString("O"),
            prescription.Notes,
            string.Join(';', prescription.Items.Select(item =>
                $"{item.MedicineName}|{item.Dosage}|{item.Frequency}|{item.Duration}|{item.Route}|{item.Instructions}")));
        var hash = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(canonicalContent)));
        prescription.Finalize(hash);

        _context.Prescriptions.Add(prescription);

        // Registra log de auditoria
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var auditLog = new AuditLog(
            userId: userId,
            userEmail: user?.Email ?? string.Empty,
            userRole: _userService.GetUserRole(),
            action: "Emitir Receita",
            entityName: "Prescription",
            entityId: prescription.Id.ToString(),
            tenantId: tenantId,
            details: $"Receita emitida com {request.Items.Count()} medicamento(s) para o agendamento {request.AppointmentId}"
        );
        _context.AuditLogs.Add(auditLog);

        await _context.SaveChangesAsync(cancellationToken);
        return prescription.Id;
    }
}
