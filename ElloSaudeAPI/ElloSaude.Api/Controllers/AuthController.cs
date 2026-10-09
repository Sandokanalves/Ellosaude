using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Infrastructure.Identity;
using ElloSaude.Application.Identity.Commands;
using MediatR;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IMediator _mediator;
    public AuthController(IUserService userService, IMediator mediator)
    {
        _userService = userService;
        _mediator = mediator;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // 1. Buscar o usuário no banco de dados usando o repositório ou serviço
        var user = await _userService.AuthenticateAsync(request.Email, request.Password, cancellationToken);

        if (user == null)
        {
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        // 2. Agora passamos os dados REAIS que vieram do banco de dados
        var token = _userService.GenerateJwtToken(user);

        return Ok(new { Token = token });
    }

    /// <summary>Changes the authenticated user's password, including first-login temporary credentials.</summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ChangePasswordCommand(request.CurrentPassword, request.NewPassword),
            cancellationToken);
        return NoContent();
    }
}

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);