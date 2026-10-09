using ElloSaude.Application.MedicalRecords.Commands;
using ElloSaude.Application.MedicalRecords.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[Authorize(Roles = "Profissional")]
[ApiController]
[Route("api/[controller]")]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MedicalRecordsController(IMediator mediator) => _mediator = mediator;

    [HttpPost]
    public async Task<IActionResult> Create(CreateMedicalRecordCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(Guid patientId)
    {
        var query = new GetRecordsByPatientQuery(patientId);
        return Ok(await _mediator.Send(query));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetMedicalRecordByIdQuery(id);
        return Ok(await _mediator.Send(query));
    }

    [HttpPost("{id:guid}/addenda")]
    public async Task<IActionResult> AddAddendum(Guid id, AddMedicalRecordAddendumRequest request)
    {
        await _mediator.Send(new AddMedicalRecordAddendumCommand(id, request.Note));
        return NoContent();
    }
}

public record AddMedicalRecordAddendumRequest(string Note);