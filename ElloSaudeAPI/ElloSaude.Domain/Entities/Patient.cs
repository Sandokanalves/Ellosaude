namespace ElloSaude.Domain.Entities;

public class Patient : BaseEntity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Cpf { get; private set; }
    public DateTime BirthDate { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? EmergencyContact { get; private set; }
    public string? Gender { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? Notes { get; private set; }
    public Guid? UserId { get; private set; }
    public User? User { get; private set; }
    public ICollection<MedicalRecord> Records { get; set; }

    public string? SocialName { get; private set; }
    public string? Profession { get; private set; }
    public string? LegalGuardian { get; private set; }
    public string? MarketingOrigin { get; private set; }
    public string? BloodType { get; private set; }
    public string? Allergies { get; private set; }
    public string? ChronicDiseases { get; private set; }
    public string? ContinuousMedications { get; private set; }
    public string? MedicalAlerts { get; private set; }

    private Patient()
    {
        Name = string.Empty;
        Email = string.Empty;
        Cpf = string.Empty;
        Records = new List<MedicalRecord>();
    }

    public Patient(string name, string email, string cpf, DateTime birthDate, string tenantId,
        string? phone = null, string? address = null, string? emergencyContact = null, string? gender = null, string? notes = null,
        string? socialName = null, string? profession = null, string? legalGuardian = null, string? marketingOrigin = null,
        string? bloodType = null, string? allergies = null, string? chronicDiseases = null, string? continuousMedications = null, string? medicalAlerts = null)
    {
        Name = name;
        Email = email;
        Cpf = cpf;
        BirthDate = birthDate;
        TenantId = tenantId;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Address = address;
        EmergencyContact = emergencyContact;
        Gender = gender;
        Notes = notes;
        SocialName = socialName;
        Profession = profession;
        LegalGuardian = legalGuardian;
        MarketingOrigin = marketingOrigin;
        BloodType = bloodType;
        Allergies = allergies;
        ChronicDiseases = chronicDiseases;
        ContinuousMedications = continuousMedications;
        MedicalAlerts = medicalAlerts;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        Records = new List<MedicalRecord>();
    }

    public void Update(string name, string email, string? phone, string? address, string? emergencyContact, string? gender, string? notes,
        string? socialName = null, string? profession = null, string? legalGuardian = null, string? marketingOrigin = null,
        string? bloodType = null, string? allergies = null, string? chronicDiseases = null, string? continuousMedications = null, string? medicalAlerts = null)
    {
        Name = name;
        Email = email;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Address = address;
        EmergencyContact = emergencyContact;
        Gender = gender;
        Notes = notes;
        SocialName = socialName;
        Profession = profession;
        LegalGuardian = legalGuardian;
        MarketingOrigin = marketingOrigin;
        BloodType = bloodType;
        Allergies = allergies;
        ChronicDiseases = chronicDiseases;
        ContinuousMedications = continuousMedications;
        MedicalAlerts = medicalAlerts;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateClinicalSummary(string? bloodType, string? allergies, string? chronicDiseases, string? continuousMedications, string? medicalAlerts)
    {
        BloodType = bloodType;
        Allergies = allergies;
        ChronicDiseases = chronicDiseases;
        ContinuousMedications = continuousMedications;
        MedicalAlerts = medicalAlerts;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePersonalData(string name, string email, string cpf, DateTime birthDate, string? phone)
    {
        Name = name;
        Email = email;
        Cpf = cpf;
        BirthDate = birthDate;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        IsDeleted = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LinkPortalAccount(User user)
    {
        if (!IsActive || IsDeleted)
            throw new InvalidOperationException("Não é possível vincular uma conta a um paciente inativo.");
        if (UserId.HasValue)
            throw new InvalidOperationException("Este paciente já possui uma conta vinculada.");
        if (user.Role != "Paciente" || user.TenantId != TenantId)
            throw new InvalidOperationException("A conta deve ter perfil de paciente e pertencer à mesma clínica.");

        UserId = user.Id;
        User = user;
        UpdatedAt = DateTime.UtcNow;
    }
}