using MediatR;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Application.Users.Queries;

public record GetStaffUsersQuery() : IRequest<IEnumerable<StaffUserDto>>;

public class GetStaffUsersHandler : IRequestHandler<GetStaffUsersQuery, IEnumerable<StaffUserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetStaffUsersHandler(IApplicationDbContext context) => _context = context;

    public async Task<IEnumerable<StaffUserDto>> Handle(GetStaffUsersQuery request, CancellationToken ct)
    {
        // Ignore deleted filter if we want to show active/inactive, or show all non-patient users
        var users = await _context.Users
            .Where(u => u.Role != "Paciente")
            .OrderBy(u => u.FullName)
            .ToListAsync(ct);

        return users.Select(u => new StaffUserDto(
            u.Id,
            u.FullName,
            u.Email,
            u.Role,
            !u.IsDeleted,
            u.CreatedAt
        ));
    }
}
