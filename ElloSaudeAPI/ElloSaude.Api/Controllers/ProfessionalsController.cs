using ElloSaude.Application.Professionals.Commands;
using ElloSaude.Application.Professionals.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElloSaude.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProfessionalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProfessionalsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Lista todos os profissionais ativos da clínica.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _mediator.Send(new GetProfessionalsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Cadastra um novo profissional de saúde.
    /// Exclusivo para Admin.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProfessionalCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetAll), new { id }, new { id });
    }

    /// <summary>
    /// Configura a disponibilidade semanal de um profissional para um dia específico.
    /// </summary>
    [HttpPost("{professionalId:guid}/availability")]
    [Authorize(Roles = "Admin,Profissional")]
    public async Task<IActionResult> SetAvailability(
        Guid professionalId,
        [FromBody] SetAvailabilityRequest request)
    {
        var id = await _mediator.Send(new SetDoctorAvailabilityCommand(
            ProfessionalId: professionalId,
            DayOfWeek: (DayOfWeek)request.DayOfWeekId,
            StartTime: TimeSpan.Parse(request.StartTime),
            EndTime: TimeSpan.Parse(request.EndTime),
            BreakStartTime: request.BreakStartTime != null ? TimeSpan.Parse(request.BreakStartTime) : null,
            BreakEndTime: request.BreakEndTime != null ? TimeSpan.Parse(request.BreakEndTime) : null,
            SlotDurationMinutes: request.SlotDurationMinutes
        ));
        return Ok(new { id, message = "Disponibilidade configurada com sucesso." });
    }

    /// <summary>
    /// Retorna os slots de horário disponíveis de um profissional para uma data específica.
    /// </summary>
    [HttpGet("{professionalId:guid}/available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        Guid professionalId,
        [FromQuery] DateOnly date)
    {
        var slots = await _mediator.Send(new GetAvailableSlotsQuery(professionalId, date));
        return Ok(slots);
    }

    [HttpPost("{professionalId:guid}/blocks")]
    [Authorize(Roles = "Admin,Profissional")]
    public async Task<IActionResult> CreateScheduleBlock(
        Guid professionalId,
        [FromBody] CreateScheduleBlockRequest request,
        CancellationToken cancellationToken)
    {
        var id = await _mediator.Send(new CreateScheduleBlockCommand(
            professionalId,
            request.StartDateTime,
            request.EndDateTime,
            request.Reason), cancellationToken);
        return CreatedAtAction(nameof(GetAvailableSlots), new { professionalId }, new { id });
    }

    [HttpDelete("{professionalId:guid}/blocks/{blockId:guid}")]
    [Authorize(Roles = "Admin,Profissional")]
    public async Task<IActionResult> CancelScheduleBlock(
        Guid professionalId,
        Guid blockId,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CancelScheduleBlockCommand(professionalId, blockId),
            cancellationToken);
        return NoContent();
    }
}

public record SetAvailabilityRequest(
    int DayOfWeekId,
    string StartTime,
    string EndTime,
    string? BreakStartTime,
    string? BreakEndTime,
    int SlotDurationMinutes
);

public record CreateScheduleBlockRequest(
    DateTime StartDateTime,
    DateTime EndDateTime,
    string Reason
);
