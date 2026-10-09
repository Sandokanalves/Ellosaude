using ElloSaude.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Identity.Commands;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty();
        RuleFor(request => request.NewPassword)
            .MinimumLength(16)
            .WithMessage("A nova senha deve conter ao menos 16 caracteres.")
            .NotEqual(request => request.CurrentPassword)
            .WithMessage("A nova senha deve ser diferente da senha atual.");
    }
}

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IHashService _hashService;
    private readonly IUserService _userService;

    public ChangePasswordHandler(
        IApplicationDbContext context,
        IHashService hashService,
        IUserService userService)
    {
        _context = context;
        _hashService = hashService;
        _userService = userService;
    }

    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = _userService.GetUserId();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Usuário autenticado inválido.");

        if (!_hashService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Senha atual inválida.");

        user.ChangePassword(_hashService.HashPassword(request.NewPassword));
        await _context.SaveChangesAsync(cancellationToken);
    }
}
