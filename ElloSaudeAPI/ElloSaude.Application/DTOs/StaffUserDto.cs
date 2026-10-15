namespace ElloSaude.Application.DTOs;

public record StaffUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt
);
