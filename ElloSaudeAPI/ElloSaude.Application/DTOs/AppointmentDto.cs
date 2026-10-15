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
    string? TenantId,
    DateTime? ArrivedAt = null,
    string? PackageSessionInfo = null,
    bool IsWalkIn = false,
    Guid? ProcedureId = null,
    string? ProcedureName = null,
    string? ColorHex = null,
    decimal Price = 0,
    bool IsFreeReturn = false,
    string? Observations = null
);
