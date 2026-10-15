namespace ElloSaude.Application.DTOs;

public record PatientDto(
    Guid Id,
    string Name,
    string Email,
    string Cpf,
    DateTime BirthDate,
    string? Phone,
    string TenantId,
    bool HasPortalAccount,
    string? SocialName = null,
    string? Address = null,
    string? EmergencyContact = null,
    string? Gender = null,
    string? Notes = null,
    string? Profession = null,
    string? LegalGuardian = null,
    string? MarketingOrigin = null,
    string? BloodType = null,
    string? Allergies = null,
    string? ChronicDiseases = null,
    string? ContinuousMedications = null,
    string? MedicalAlerts = null,
    bool IsActive = true
);