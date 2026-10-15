namespace ElloSaude.Application.DTOs;

public record ClinicalAttachmentDto(
    Guid Id,
    Guid PatientId,
    Guid ProfessionalId,
    Guid? AppointmentId,
    string Title,
    string Category,
    string ContentType,
    string FileUrl,
    string? Notes,
    DateTime CreatedAt
);
