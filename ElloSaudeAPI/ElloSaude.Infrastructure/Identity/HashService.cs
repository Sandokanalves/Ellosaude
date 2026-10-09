using ElloSaude.Application.Common.Interfaces;
using BC = BCrypt.Net.BCrypt;

namespace ElloSaude.Infrastructure.Identity;

public class HashService : IHashService
{
    // Gera um hash seguro (Salt já está incluso no método)
    public string HashPassword(string password)
        => BC.HashPassword(password);

    // Verifica se a senha digitada bate com o hash do banco
    public bool VerifyPassword(string password, string hash)
        => BC.Verify(password, hash);
}