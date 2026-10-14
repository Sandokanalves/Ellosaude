using ElloSaude.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Professionals.Queries;

/// <summary>Active schedule block visible to authorized clinic staff.</summary>
public sealed record ScheduleBlockDto(
    Guid Id,
    Guid ProfessionalId,
    string ProfessionalName,
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime,
    string Reason);

public sealed record GetScheduleBlocksQuery(
    DateTimeOffset StartDateTime,
    DateTimeOffset EndDateTime,
    Guid? ProfessionalId) : IRequest<IReadOnlyList<ScheduleBlockDto>>;

public sealed class GetScheduleBlocksQueryHandler
    : IRequestHandler<GetScheduleBlocksQuery, IReadOnlyList<ScheduleBlockDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUserService _userService;

    public GetScheduleBlocksQueryHandler(
        IApplicationDbContext context,
        IUserService userService)
    {
        _context = context;
        _userService = userService;
    }

    public async Task<IReadOnlyList<ScheduleBlockDto>> Handle(
        GetScheduleBlocksQuery request,
        CancellationToken cancellationToken)
    {
        if (request.StartDateTime >= request.EndDateTime)
            throw new ArgumentException("O início do período deve ser anterior ao término.");

        var query = _context.ScheduleBlocks
            .AsNoTracking()
            .Where(block => block.IsActive
                && block.StartDateTime < request.EndDateTime.UtcDateTime
                && block.EndDateTime > request.StartDateTime.UtcDateTime);

        if (request.ProfessionalId.HasValue)
            query = query.Where(block => block.ProfessionalId == request.ProfessionalId.Value);

        var role = _userService.GetUserRole();
        if (role == "Profissional")
        {
            var userId = _userService.GetUserId();
            if (userId == Guid.Empty)
                throw new UnauthorizedAccessException("Usuário profissional autenticado inválido.");

            var professionalIds = await _context.Professionals
                .Where(professional => professional.UserId == userId && professional.IsActive)
                .Select(professional => professional.Id)
                .ToListAsync(cancellationToken);
            if (professionalIds.Count == 0)
                throw new UnauthorizedAccessException("A conta não está vinculada a um profissional ativo.");

            query = query.Where(block => professionalIds.Contains(block.ProfessionalId));

            if (request.ProfessionalId.HasValue
                && !professionalIds.Contains(request.ProfessionalId.Value))
                throw new KeyNotFoundException("Profissional não encontrado.");
        }
        else if (role is not ("Admin" or "Secretaria"))
        {
            throw new UnauthorizedAccessException("O usuário não pode consultar bloqueios de agenda.");
        }

        return await query
            .Join(
                _context.Professionals.AsNoTracking(),
                block => block.ProfessionalId,
                professional => professional.Id,
                (block, professional) => new ScheduleBlockDto(
                    block.Id,
                    block.ProfessionalId,
                    professional.Name,
                    new DateTimeOffset(DateTime.SpecifyKind(block.StartDateTime, DateTimeKind.Utc)),
                    new DateTimeOffset(DateTime.SpecifyKind(block.EndDateTime, DateTimeKind.Utc)),
                    block.Reason))
            .OrderBy(block => block.StartDateTime)
            .ToListAsync(cancellationToken);
    }
}
