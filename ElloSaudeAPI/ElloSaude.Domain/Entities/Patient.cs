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
    public ICollection<MedicalRecord> Records { get; set; }

    private Patient()
    {
        Name = string.Empty;
        Email = string.Empty;
        Cpf = string.Empty;
        Records = new List<MedicalRecord>();
    }

    public Patient(string name, string email, string cpf, DateTime birthDate, string tenantId,
        string? phone = null, string? address = null, string? emergencyContact = null, string? gender = null, string? notes = null)
    {
        Name = name;
        Email = email;
        Cpf = cpf;
        BirthDate = birthDate;
        TenantId = tenantId;
        Phone = phone;
        Address = address;
        EmergencyContact = emergencyContact;
        Gender = gender;
        Notes = notes;
        IsActive = true;
        Records = new List<MedicalRecord>();
    }

    public void Update(string name, string email, string? phone, string? address, string? emergencyContact, string? gender, string? notes)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Address = address;
        EmergencyContact = emergencyContact;
        Gender = gender;
        Notes = notes;
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
}