using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    public AuthController(IUserService userService) => _userService = userService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Buscar o usuário no banco de dados usando o repositório ou serviço
        var user = await _userService.AuthenticateAsync(request.Email, request.Password, CancellationToken.None);

        if (user == null)
        {
            return Unauthorized(new { message = "Credenciais inválidas." });
        }

        // 2. Agora passamos os dados REAIS que vieram do banco de dados
        var token = _userService.GenerateJwtToken(user);

        return Ok(new { Token = token });
    }
}