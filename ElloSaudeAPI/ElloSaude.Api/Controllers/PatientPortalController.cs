using ElloSaude.Application.Appointments.Commands;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.PatientPortal;
using ElloSaude.Application.Prescriptions.Queries;
using ElloSaude.Application.Professionals.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ElloSaude.Domain.Enums;

namespace ElloSaude.Api.Controllers;

/// <summary>Endpoints restricted to the patient account explicitly linked by the clinic.</summary>
[ApiController]
[Authorize(Roles = "Paciente")]
[Route("api/patient-portal")]
public sealed class PatientPortalController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPrescriptionPdfService _pdfService;

    public PatientPortalController(IMediator mediator, IPrescriptionPdfService pdfService)
    {
        _mediator = mediator;
        _pdfService = pdfService;
    }

    /// <summary>Lists only the authenticated patient's appointments.</summary>
    [HttpGet("my-appointments")]
    public async Task<ActionResult<IReadOnlyCollection<PatientAppointmentDto>>> GetMyAppointments(
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetMyAppointmentsQuery(), cancellationToken));

    /// <summary>Returns one appointment only when it belongs to the authenticated patient.</summary>
    [HttpGet("my-appointments/{id:guid}")]
    public async Task<ActionResult<PatientAppointmentDto>> GetMyAppointment(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetMyAppointmentQuery(id), cancellationToken));

    /// <summary>Lists open slots for a professional on a date.</summary>
    [HttpGet("availability")]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid professionalId,
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var slots = await _mediator.Send(
            new GetAvailableSlotsQuery(professionalId, date),
            cancellationToken);
        return Ok(slots.Where(slot => slot.IsAvailable));
    }

    /// <summary>Books an available standard consultation for the authenticated patient.</summary>
    [HttpPost("book")]
    public async Task<IActionResult> Book(
        [FromBody] BookPatientAppointmentRequest request,
        CancellationToken cancellationToken)
    {
        var patientId = await _mediator.Send(new GetMyPatientIdQuery(), cancellationToken);
        var id = await _mediator.Send(new CreateAppointmentCommand(
            patientId,
            request.ProfessionalId,
            request.Start.UtcDateTime,
            request.End.UtcDateTime,
            (int)AppointmentType.Consulta), cancellationToken);
        return CreatedAtAction(nameof(GetMyAppointment), new { id }, new { id });
    }

    /// <summary>Cancels an upcoming appointment owned by the authenticated patient.</summary>
    [HttpPost("cancel/{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new CancelMyAppointmentCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>Lists only issued prescriptions belonging to the authenticated patient.</summary>
    [HttpGet("my-prescriptions")]
    public async Task<ActionResult<IReadOnlyCollection<PatientPrescriptionDto>>> GetMyPrescriptions(
        CancellationToken cancellationToken) =>
        Ok(await _mediator.Send(new GetMyPrescriptionsQuery(), cancellationToken));

    /// <summary>Downloads an issued prescription belonging to the authenticated patient.</summary>
    [HttpGet("my-prescriptions/{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPrescription(
        Guid id,
        CancellationToken cancellationToken)
    {
        var prescription = await _mediator.Send(new GetPrescriptionDetailsQuery(id), cancellationToken);
        return File(_pdfService.Generate(prescription), "application/pdf", $"receita-{id:N}.pdf");
    }
}

/// <summary>Requested professional and slot for a patient-booked appointment.</summary>
public sealed record BookPatientAppointmentRequest(
    Guid ProfessionalId,
    DateTimeOffset Start,
    DateTimeOffset End);
