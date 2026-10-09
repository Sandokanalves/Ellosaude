namespace ElloSaude.Domain.Entities;

public class Patient : BaseEntity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string Cpf { get; private set; }
    public DateTime BirthDate { get; private set; }
    public ICollection<MedicalRecord> Records { get; set; }

    public Patient(string name, string email, string cpf, DateTime birthDate, string tenantId)
    {
        Name = name;
        Email = email;
        Cpf = cpf;
        BirthDate = birthDate;
        TenantId = tenantId;
        Records = new List<MedicalRecord>();
    }
}