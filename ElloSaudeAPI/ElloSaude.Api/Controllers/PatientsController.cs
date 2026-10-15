using ElloSaude.Application.ClinicalAttachments.Commands;
using ElloSaude.Application.ClinicalAttachments.Queries;
using ElloSaude.Application.ClinicalMarkers.Commands;
using ElloSaude.Application.ClinicalMarkers.Queries;
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

    [HttpPatch("{id}/clinical-summary")]
    [Authorize(Roles = "Profissional")]
    public async Task<IActionResult> UpdateClinicalSummary(Guid id, UpdatePatientClinicalSummaryCommand command, CancellationToken cancellationToken)
    {
        if (id != command.PatientId) return BadRequest(new { message = "ID do paciente diverge da rota." });
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

    // --- Marcadores Visuais Interativos (Mapa Corporal / Facial) ---

    [HttpGet("{id:guid}/markers")]
    [Authorize(Roles = "Profissional")]
    public async Task<IActionResult> GetMarkers(Guid id, [FromQuery] string? target, CancellationToken ct)
    {
        var markers = await _mediator.Send(new GetClinicalMarkersQuery(id, target), ct);
        return Ok(markers);
    }

    [HttpPost("{id:guid}/markers")]
    [Authorize(Roles = "Profissional")]
    public async Task<IActionResult> SaveMarker(Guid id, SaveClinicalMarkerCommand command, CancellationToken ct)
    {
        if (id != command.PatientId) return BadRequest("ID do paciente não confere.");
        var markerId = await _mediator.Send(command, ct);
        return Ok(new { Id = markerId });
    }

    [HttpDelete("{id:guid}/markers/{markerId:guid}")]
    [Authorize(Roles = "Profissional")]
    public async Task<IActionResult> DeleteMarker(Guid id, Guid markerId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteClinicalMarkerCommand(markerId), ct);
        return NoContent();
    }

    // --- Anexos Clínicos (Exames, Laudos, Fotos Antes e Depois) ---

    [HttpGet("{id:guid}/attachments")]
    [Authorize(Roles = "Admin,Profissional")]
    public async Task<IActionResult> GetAttachments(Guid id, [FromQuery] string? category, CancellationToken ct)
    {
        var attachments = await _mediator.Send(new GetClinicalAttachmentsQuery(id, category), ct);
        return Ok(attachments);
    }

    [HttpPost("{id:guid}/attachments")]
    [Authorize(Roles = "Admin,Profissional")]
    public async Task<IActionResult> CreateAttachment(Guid id, CreateClinicalAttachmentCommand command, CancellationToken ct)
    {
        if (id != command.PatientId) return BadRequest("ID do paciente não confere.");
        var attachmentId = await _mediator.Send(command, ct);
        return Ok(new { Id = attachmentId });
    }

    [HttpDelete("{id:guid}/attachments/{attachmentId:guid}")]
    [Authorize(Roles = "Admin,Profissional")]
    public async Task<IActionResult> DeleteAttachment(Guid id, Guid attachmentId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteClinicalAttachmentCommand(attachmentId), ct);
        return NoContent();
    }
}

/// <summary>Portal credentials to be handed to the patient through a separate secure channel.</summary>
public sealed record PatientPortalAccountResponse(string Email, string TemporaryPassword);