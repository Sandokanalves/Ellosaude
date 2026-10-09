using ElloSaude.Domain.Enums;

namespace ElloSaude.Application.DTOs;

public record AppointmentDto(
    Guid Id,
    Guid PatientId,
    string? PatientName,
    Guid ProfessionalId,
    string? ProfessionalName,
    DateTime StartTime,
    DateTime EndTime,
    AppointmentType Type,
    AppointmentStatus Status,
    string? TenantId
);
