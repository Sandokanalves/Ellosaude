namespace ElloSaude.Domain.Entities;

public class Professional : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Name { get; private set; }
    public string Crm { get; private set; }
    public string CrmState { get; private set; }
    public string Specialty { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public decimal ConsultationPrice { get; private set; } = 150.00m;
    public bool IsActive { get; private set; } = true;

    public ICollection<DoctorAvailability> Availabilities { get; private set; }
    public ICollection<ScheduleBlock> ScheduleBlocks { get; private set; }

    private Professional()
    {
        Name = string.Empty;
        Crm = string.Empty;
        CrmState = string.Empty;
        Specialty = string.Empty;
        Availabilities = new List<DoctorAvailability>();
        ScheduleBlocks = new List<ScheduleBlock>();
    }

    public Professional(string name, string crm, string crmState, string specialty,
        string tenantId, Guid? userId = null, string? phone = null, string? email = null, decimal consultationPrice = 150.00m)
    {
        Name = name;
        Crm = crm;
        CrmState = crmState;
        Specialty = specialty;
        TenantId = tenantId;
        UserId = userId;
        Phone = phone;
        Email = email;
        ConsultationPrice = consultationPrice;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        Availabilities = new List<DoctorAvailability>();
        ScheduleBlocks = new List<ScheduleBlock>();
    }

    public void Update(string name, string crm, string crmState, string specialty, string? phone, string? email, decimal consultationPrice)
    {
        Name = name;
        Crm = crm;
        CrmState = crmState;
        Specialty = specialty;
        Phone = phone;
        Email = email;
        ConsultationPrice = consultationPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
