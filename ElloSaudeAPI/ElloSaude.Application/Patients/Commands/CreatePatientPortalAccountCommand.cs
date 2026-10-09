using System.Security.Cryptography;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Patients.Commands;

public sealed record CreatePatientPortalAccountCommand(Guid PatientId) : IRequest<PatientPortalAccountCredentials>;

public sealed record PatientPortalAccountCredentials(string Email, string TemporaryPassword);

public sealed class CreatePatientPortalAccountValidator : AbstractValidator<CreatePatientPortalAccountCommand>
{
    public CreatePatientPortalAccountValidator()
    {
        RuleFor(request => request.PatientId).NotEmpty();
    }
}

public sealed class CreatePatientPortalAccountHandler
    : IRequestHandler<CreatePatientPortalAccountCommand, PatientPortalAccountCredentials>
{
    private readonly IApplicationDbContext _context;
    private readonly IHashService _hashService;
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;

    public CreatePatientPortalAccountHandler(
        IApplicationDbContext context,
        IHashService hashService,
        ITenantService tenantService,
        IUserService userService)
    {
        _context = context;
        _hashService = hashService;
        _tenantService = tenantService;
        _userService = userService;
    }

    public async Task<PatientPortalAccountCredentials> Handle(
        CreatePatientPortalAccountCommand request,
        CancellationToken cancellationToken)
    {
        var patient = await _context.Patients
            .FirstOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken)
            ?? throw new KeyNotFoundException("Paciente não encontrado.");

        if (patient.UserId.HasValue)
            throw new InvalidOperationException("Este paciente já possui uma conta vinculada.");

        var email = patient.Email.Trim().ToLowerInvariant();
        if (await _context.Users.IgnoreQueryFilters()
                .AnyAsync(user => user.Email == email, cancellationToken))
            throw new InvalidOperationException("O e-mail já está associado a outra conta.");

        var temporaryPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tenantId = _tenantService.GetTenantId();
        var user = new User(
            email,
            _hashService.HashPassword(temporaryPassword),
            tenantId,
            "Paciente",
            mustChangePassword: true);

        patient.LinkPortalAccount(user);
        _context.Users.Add(user);
        var operatorId = _userService.GetUserId();
        var operatorUser = await _context.Users
            .FirstOrDefaultAsync(existing => existing.Id == operatorId, cancellationToken);
        _context.AuditLogs.Add(new AuditLog(
            operatorId,
            operatorUser?.Email ?? string.Empty,
            _userService.GetUserRole(),
            "Criar conta do portal do paciente",
            nameof(Patient),
            patient.Id.ToString(),
            tenantId));

        await _context.SaveChangesAsync(cancellationToken);
        return new PatientPortalAccountCredentials(email, temporaryPassword);
    }
}
