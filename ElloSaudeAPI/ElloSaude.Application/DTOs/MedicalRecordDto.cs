namespace ElloSaude.Application.DTOs;

public record MedicalRecordDto(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    string Description,
    string Diagnosis,
    DateTime CreatedAt
);