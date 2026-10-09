using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ElloSaude.Application.Clinics.Commands;

public record RegisterClinicCommand(string Name, string Cnpj, string AdminEmail, string Password) : IRequest<Guid>;

public class RegisterClinicValidator : AbstractValidator<RegisterClinicCommand>
{
    public RegisterClinicValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Cnpj).NotEmpty();
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class RegisterClinicHandler : IRequestHandler<RegisterClinicCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly IHashService _hashService;

    public RegisterClinicHandler(IUnitOfWork uow, IHashService hashService)
    {
        _uow = uow;
        _hashService = hashService;
    }

    public async Task<Guid> Handle(RegisterClinicCommand request, CancellationToken ct)
    {
        var tenantId = Guid.NewGuid().ToString();

        var clinic = new Clinic(request.Name, request.Cnpj, tenantId);

        var passwordHash = _hashService.HashPassword(request.Password);
        var user = new User(request.AdminEmail, passwordHash, tenantId, "Profissional");

        await _uow.Clinics.AddAsync(clinic, ct);
        await _uow.Users.AddAsync(user, ct);

        await _uow.CompleteAsync(ct);

        return clinic.Id;
    }
}