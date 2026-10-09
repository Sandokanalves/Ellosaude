using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Professionals.Commands;

public record CreateScheduleBlockCommand(
    Guid ProfessionalId,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Reason
) : IRequest<Guid>;

public class CreateScheduleBlockCommandValidator : AbstractValidator<CreateScheduleBlockCommand>
{
    public CreateScheduleBlockCommandValidator()
    {
        RuleFor(x => x.ProfessionalId).NotEmpty();
        RuleFor(x => x.StartDateTime).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.EndDateTime).GreaterThan(x => x.StartDateTime);
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class CreateScheduleBlockCommandHandler : IRequestHandler<CreateScheduleBlockCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IUserService _userService;

    public CreateScheduleBlockCommandHandler(
        IApplicationDbContext context,
        ITenantService tenantService,
        IUserService userService)
    {
        _context = context;
        _tenantService = tenantService;
        _userService = userService;
    }

    public async Task<Guid> Handle(
        CreateScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        var professional = await _context.Professionals
            .FirstOrDefaultAsync(p => p.Id == request.ProfessionalId && p.IsActive, cancellationToken)
            ?? throw new KeyNotFoundException("Profissional não encontrado.");

        EnsureCanManageProfessional(professional);

        var block = new ScheduleBlock(
            professional.Id,
            request.StartDateTime,
            request.EndDateTime,
            request.Reason,
            _tenantService.GetTenantId());
        _context.ScheduleBlocks.Add(block);
        await _context.SaveChangesAsync(cancellationToken);
        return block.Id;
    }

    private void EnsureCanManageProfessional(Professional professional)
    {
        if (_userService.GetUserRole() != "Admin"
            && professional.UserId != _userService.GetUserId())
        {
            throw new UnauthorizedAccessException("O usuário não pode alterar a agenda deste profissional.");
        }
    }
}

public record CancelScheduleBlockCommand(Guid ProfessionalId, Guid ScheduleBlockId) : IRequest;

public class CancelScheduleBlockCommandHandler : IRequestHandler<CancelScheduleBlockCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public CancelScheduleBlockCommandHandler(
        IApplicationDbContext context,
        IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task Handle(
        CancelScheduleBlockCommand request,
        CancellationToken cancellationToken)
    {
        var block = await _context.ScheduleBlocks
            .FirstOrDefaultAsync(
                b => b.Id == request.ScheduleBlockId && b.ProfessionalId == request.ProfessionalId,
                cancellationToken)
            ?? throw new KeyNotFoundException("Bloqueio de agenda não encontrado.");

        var professional = await _context.Professionals
            .FirstOrDefaultAsync(p => p.Id == block.ProfessionalId, cancellationToken)
            ?? throw new KeyNotFoundException("Profissional não encontrado.");

        if (_userService.GetUserRole() != "Admin"
            && professional.UserId != _userService.GetUserId())
        {
            throw new UnauthorizedAccessException("O usuário não pode alterar a agenda deste profissional.");
        }

        block.Cancel();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
