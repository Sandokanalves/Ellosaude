using ElloSaude.Application.Patients.Commands;
using ElloSaude.Application.Patients.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[Authorize(Roles = "Admin,Profissional,Secretaria")]
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PatientsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreatePatientCommand command, CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetAllPatientsQuery(), cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetPatientByIdQuery(id), cancellationToken));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdatePatientCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id) return BadRequest(new { message = "ID no corpo diverge do parâmetro da rota." });
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeletePatientCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Creates a patient account and returns its one-time temporary password.</summary>
    [HttpPost("{id:guid}/portal-account")]
    public async Task<ActionResult<PatientPortalAccountResponse>> CreatePortalAccount(
        Guid id,
        CancellationToken cancellationToken)
    {
        var credentials = await _mediator.Send(new CreatePatientPortalAccountCommand(id), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        Response.Headers.Pragma = "no-cache";
        return Ok(new PatientPortalAccountResponse(credentials.Email, credentials.TemporaryPassword));
    }
}

/// <summary>Portal credentials to be handed to the patient through a separate secure channel.</summary>
public sealed record PatientPortalAccountResponse(string Email, string TemporaryPassword);