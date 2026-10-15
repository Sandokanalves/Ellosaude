using ElloSaude.Application.MedicalDocuments.Commands;
using ElloSaude.Application.MedicalDocuments.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Profissional")]
public class MedicalDocumentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MedicalDocumentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetByPatient(Guid patientId)
    {
        var docs = await _mediator.Send(new GetMedicalDocumentsByPatientQuery(patientId));
        return Ok(docs);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var doc = await _mediator.Send(new GetMedicalDocumentByIdQuery(id));
        return Ok(doc);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateMedicalDocumentCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, new { Id = id });
    }
}
