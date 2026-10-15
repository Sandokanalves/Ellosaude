namespace ElloSaude.Application.DTOs;

public record MedicalDocumentDto(
    Guid Id,
    Guid PatientId,
    Guid ProfessionalId,
    Guid? AppointmentId,
    string DocumentType,
    string Title,
    string Content,
    string? Cid,
    int? DaysOff,
    DateTime CreatedAt,
    string? ProfessionalName = null,
    string? ProfessionalCrm = null,
    string? ProfessionalCrmState = null,
    string? ProfessionalCouncil = null
);
