using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Infrastructure.Identity;

public class UserService : IUserService
{
    private readonly IApplicationDbContext _context;
    private readonly IHashService _hashService;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(
        IApplicationDbContext context,
        IHashService hashService,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _hashService = hashService;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return userId != null ? Guid.Parse(userId) : Guid.Empty;
    }

    public string GetUserRole()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
    }

    public async Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Email == email, ct);
        if (user == null) return null;
        return _hashService.VerifyPassword(password, user.PasswordHash) ? user : null;
    }

    public string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"] ?? "Chave_Super_Secreta_Com_Pelo_Menos_32_Caracteres");
        var issuer = _configuration["Jwt:Issuer"] ?? "ElloSaudeAPI";
        var audience = _configuration["Jwt:Audience"] ?? "ElloSaudeClientes";

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("TenantId", user.TenantId ?? string.Empty)
            }),
            Issuer = issuer,
            Audience = audience,
            Expires = DateTime.UtcNow.AddHours(8),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}