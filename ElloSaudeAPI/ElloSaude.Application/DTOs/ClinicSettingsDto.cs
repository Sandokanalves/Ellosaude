namespace ElloSaude.Application.DTOs;

public record ClinicSettingsDto(
    Guid Id,
    string Name,
    string Cnpj,
    string NameFantasy,
    string? Phone,
    string? Email,
    string? Address,
    string? City,
    string? State,
    string? LogoUrl,
    int MaxReturnDays,
    decimal DefaultCommissionPercentage,
    string TenantId
);
