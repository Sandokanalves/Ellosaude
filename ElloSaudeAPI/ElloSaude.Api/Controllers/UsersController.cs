using ElloSaude.Application.Users.Commands;
using ElloSaude.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("staff")]
    [Authorize(Roles = "Admin,Secretaria")]
    public async Task<IActionResult> GetStaff()
    {
        var staff = await _mediator.Send(new GetStaffUsersQuery());
        return Ok(staff);
    }

    [HttpPost("staff")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateStaff(CreateStaffUserCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetStaff), new { id }, new { Id = id });
    }

    [HttpPatch("staff/{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStaffStatus(Guid id)
    {
        await _mediator.Send(new ToggleStaffUserStatusCommand(id));
        return NoContent();
    }
}
