using ElloSaude.Domain.Entities;

namespace ElloSaude.Application.Common.Interfaces;

public interface IUserService
{
    Task<User?> AuthenticateAsync(string email, string password, CancellationToken ct);
    string GenerateJwtToken(User user);
    Guid GetUserId();
    string GetUserRole();   // Retorna o ID do Usuário (Médico) logado
}