namespace ElloSaude.Application.DTOs;

public record ProcedureDto(
    Guid Id,
    string Name,
    string Specialty,
    decimal Price,
    int DurationMinutes,
    string ColorHex,
    bool IsActive
);
