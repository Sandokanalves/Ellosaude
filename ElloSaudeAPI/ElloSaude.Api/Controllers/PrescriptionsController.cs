using ElloSaude.Application.Prescriptions.Commands;
using ElloSaude.Application.Prescriptions.Queries;
using ElloSaude.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PrescriptionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IPrescriptionPdfService _pdfService;

    public PrescriptionsController(IMediator mediator, IPrescriptionPdfService pdfService)
    {
        _mediator = mediator;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Emite uma nova receita médica vinculada a uma consulta.
    /// Exclusivo para o profissional responsável (Profissional).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Profissional")]
    public async Task<IActionResult> Create([FromBody] CreatePrescriptionRequest request)
    {
        var id = await _mediator.Send(new CreatePrescriptionCommand(
            AppointmentId: request.AppointmentId,
            ProfessionalId: request.ProfessionalId,
            PatientId: request.PatientId,
            GeneralNotes: request.GeneralNotes,
            Items: request.Items.Select(i => new PrescriptionItemInput(
                MedicationName: i.MedicationName,
                Dosage: i.Dosage,
                Frequency: i.Frequency,
                Duration: i.Duration,
                Route: i.Route,
                Instructions: i.Instructions
            ))
        ));

        return CreatedAtAction(nameof(GetById), new { id }, new { id, message = "Receita emitida com sucesso." });
    }

    /// <summary>
    /// Retorna os dados de uma receita pelo ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Profissional")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetPrescriptionDetailsQuery(id), cancellationToken));
    }

    /// <summary>
    /// Gera o PDF da receita para download.
    /// Permitido somente ao profissional emissor.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    [Authorize(Roles = "Profissional")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken cancellationToken)
    {
        var prescription = await _mediator.Send(new GetPrescriptionDetailsQuery(id), cancellationToken);
        var pdf = _pdfService.Generate(prescription);
        return File(pdf, "application/pdf", $"receita-{id:N}.pdf");
    }
}

public record CreatePrescriptionRequest(
    Guid AppointmentId,
    Guid ProfessionalId,
    Guid PatientId,
    string? GeneralNotes,
    IEnumerable<PrescriptionItemRequest> Items
);

public record PrescriptionItemRequest(
    string MedicationName,
    string Dosage,
    string Frequency,
    string Duration,
    string Route,
    string? Instructions
);
