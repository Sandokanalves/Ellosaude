namespace ElloSaude.Application.DTOs;

public record PatientDto(
    Guid Id,
    string Name,
    string Email,
    string Cpf,
    DateTime BirthDate,
    string TenantId
);