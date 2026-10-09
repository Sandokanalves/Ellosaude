using ElloSaude.Application.MedicalRecords.Commands;
using ElloSaude.Application.MedicalRecords.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MedicalRecordsController(IMediator mediator) => _mediator = mediator;

    [Authorize(Roles = "Profissional, Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateMedicalRecordCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [Authorize(Roles = "Profissional, Admin")]
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(Guid patientId)
    {
        var query = new GetRecordsByPatientQuery(patientId);
        return Ok(await _mediator.Send(query));
    }

    [Authorize(Roles = "Profissional, Admin")]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetMedicalRecordByIdQuery(id);
        return Ok(await _mediator.Send(query));
    }

    [Authorize(Roles = "Profissional, Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, UpdateMedicalRecordCommand command)
    {
        if (id != command.Id) return BadRequest(new { message = "ID no corpo diverge do parâmetro da rota." });
        await _mediator.Send(command);
        return NoContent();
    }

    [Authorize(Roles = "Profissional, Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteMedicalRecordCommand(id));
        return NoContent();
    }
}