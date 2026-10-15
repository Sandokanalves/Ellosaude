using ElloSaude.Application.Clinics.Commands;
using ElloSaude.Application.Clinics.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClinicsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ClinicsController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterClinicCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { ClinicId = id, Message = "Clínica e Administrador criados com sucesso." });
    }

    [HttpGet("settings")]
    [Authorize(Roles = "Admin,Profissional,Secretaria")]
    public async Task<IActionResult> GetSettings()
    {
        var settings = await _mediator.Send(new GetClinicSettingsQuery());
        return Ok(settings);
    }

    [HttpPut("settings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSettings(UpdateClinicSettingsCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpGet("procedures")]
    [Authorize]
    public async Task<IActionResult> GetProcedures([FromQuery] string? specialty = null, [FromQuery] bool activeOnly = false)
    {
        var procedures = await _mediator.Send(new GetProceduresQuery(specialty, activeOnly));
        return Ok(procedures);
    }

    [HttpPost("procedures")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateProcedure(CreateProcedureCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProcedures), new { id }, new { Id = id });
    }

    [HttpPut("procedures/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProcedure(Guid id, UpdateProcedureCommand command)
    {
        if (id != command.Id)
            return BadRequest("O identificador do procedimento não confere.");

        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPatch("procedures/{id}/toggle-status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleProcedureStatus(Guid id)
    {
        await _mediator.Send(new ToggleProcedureStatusCommand(id));
        return NoContent();
    }
}