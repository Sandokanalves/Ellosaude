namespace ElloSaude.Application.DTOs;

public record ClinicDto(Guid Id, string Name, string Cnpj, string TenantId);
public record RegisterClinicRequest(string Name, string Cnpj, string AdminEmail, string Password);