using ElloSaude.Application.Appointments.Commands;
using ElloSaude.Application.Appointments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateAppointmentCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        return Ok(await _mediator.Send(new GetAllAppointmentsQuery(startDate, endDate)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        return Ok(await _mediator.Send(new GetAppointmentByIdQuery(id)));
    }

    [HttpGet("professional/{professionalId}")]
    public async Task<IActionResult> GetByProfessional(Guid professionalId, [FromQuery] DateTime date)
    {
        return Ok(await _mediator.Send(new GetAgendaByProfessionalQuery(professionalId, date)));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateAppointmentStatusCommand command)
    {
        if (id != command.Id) return BadRequest(new { message = "ID no corpo diverge do parâmetro da rota." });
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteAppointmentCommand(id));
        return NoContent();
    }
}