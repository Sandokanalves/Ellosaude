using ElloSaude.Application.Clinics.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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

}