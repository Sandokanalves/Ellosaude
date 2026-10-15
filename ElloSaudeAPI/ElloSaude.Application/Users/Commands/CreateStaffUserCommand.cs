using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Users.Commands;

public record CreateStaffUserCommand(
    string FullName,
    string Email,
    string Password,
    string Role) : IRequest<Guid>;

public class CreateStaffUserValidator : AbstractValidator<CreateStaffUserCommand>
{
    public CreateStaffUserValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.Role).Must(r => r is "Admin" or "Profissional" or "Secretaria")
            .WithMessage("O perfil deve ser Admin, Profissional ou Secretaria.");
    }
}

public class CreateStaffUserHandler : IRequestHandler<CreateStaffUserCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;
    private readonly IHashService _hashService;

    public CreateStaffUserHandler(IApplicationDbContext context, ITenantService tenant, IHashService hashService)
    {
        _context = context;
        _tenant = tenant;
        _hashService = hashService;
    }

    public async Task<Guid> Handle(CreateStaffUserCommand request, CancellationToken ct)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var existing = await _context.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (existing)
            throw new InvalidOperationException("Já existe um usuário cadastrado com este e-mail.");

        var passwordHash = _hashService.HashPassword(request.Password);
        var user = new User(
            normalizedEmail,
            passwordHash,
            _tenant.GetTenantId(),
            request.Role,
            mustChangePassword: false,
            fullName: request.FullName
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
        return user.Id;
    }
}

public record ToggleStaffUserStatusCommand(Guid Id) : IRequest;

public class ToggleStaffUserStatusHandler : IRequestHandler<ToggleStaffUserStatusCommand>
{
    private readonly IApplicationDbContext _context;

    public ToggleStaffUserStatusHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(ToggleStaffUserStatusCommand request, CancellationToken ct)
    {
        var user = await _context.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == request.Id, ct)
            ?? throw new KeyNotFoundException("Colaborador não encontrado.");

        if (user.IsDeleted)
            user.Activate();
        else
            user.Deactivate();

        await _context.SaveChangesAsync(ct);
    }
}
