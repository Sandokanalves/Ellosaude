using FluentValidation;
using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Clinics.Commands;

public record CreateProcedureCommand(
    string Name,
    string Specialty,
    decimal Price,
    int DurationMinutes,
    string ColorHex) : IRequest<Guid>;

public class CreateProcedureValidator : AbstractValidator<CreateProcedureCommand>
{
    public CreateProcedureValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Specialty).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
    }
}

public class CreateProcedureHandler : IRequestHandler<CreateProcedureCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenant;

    public CreateProcedureHandler(IApplicationDbContext context, ITenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    public async Task<Guid> Handle(CreateProcedureCommand request, CancellationToken ct)
    {
        var procedure = new Procedure(
            request.Name,
            request.Specialty,
            request.Price,
            request.DurationMinutes,
            request.ColorHex,
            _tenant.GetTenantId()
        );

        _context.Procedures.Add(procedure);
        await _context.SaveChangesAsync(ct);
        return procedure.Id;
    }
}

public record UpdateProcedureCommand(
    Guid Id,
    string Name,
    string Specialty,
    decimal Price,
    int DurationMinutes,
    string ColorHex) : IRequest;

public class UpdateProcedureValidator : AbstractValidator<UpdateProcedureCommand>
{
    public UpdateProcedureValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationMinutes).GreaterThan(0);
    }
}

public class UpdateProcedureHandler : IRequestHandler<UpdateProcedureCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateProcedureHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateProcedureCommand request, CancellationToken ct)
    {
        var procedure = await _context.Procedures.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Procedimento não encontrado.");

        procedure.Update(request.Name, request.Specialty, request.Price, request.DurationMinutes, request.ColorHex);
        await _context.SaveChangesAsync(ct);
    }
}

public record ToggleProcedureStatusCommand(Guid Id) : IRequest;

public class ToggleProcedureStatusHandler : IRequestHandler<ToggleProcedureStatusCommand>
{
    private readonly IApplicationDbContext _context;

    public ToggleProcedureStatusHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(ToggleProcedureStatusCommand request, CancellationToken ct)
    {
        var procedure = await _context.Procedures.FindAsync(new object[] { request.Id }, ct)
            ?? throw new KeyNotFoundException("Procedimento não encontrado.");

        procedure.ToggleStatus();
        await _context.SaveChangesAsync(ct);
    }
}
