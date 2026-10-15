namespace ElloSaude.Application.DTOs;

public record ClinicalMarkerDto(
    Guid Id,
    Guid PatientId,
    Guid? AppointmentId,
    Guid ProfessionalId,
    string Target,
    decimal X,
    decimal Y,
    string Type,
    string Label,
    string? IntensityOrDose,
    string? Notes,
    DateTime CreatedAt
);
