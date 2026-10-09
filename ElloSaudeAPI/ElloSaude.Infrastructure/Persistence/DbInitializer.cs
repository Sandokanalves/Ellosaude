using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context, IHashService hashService)
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.Clinics.IgnoreQueryFilters().AnyAsync())
        {
            var tenantId = Guid.NewGuid().ToString();
            var clinic = new Clinic("Clínica ElloSaúde", "12.345.678/0001-90", tenantId);
            context.Clinics.Add(clinic);

            var passwordHash = hashService.HashPassword("Senha123!");
            var adminUser = new User("admin@ellosaude.com", passwordHash, tenantId, "Profissional");
            context.Users.Add(adminUser);

            var doctorUser = new User("doutor@ellosaude.com", passwordHash, tenantId, "Profissional");
            context.Users.Add(doctorUser);

            var secretaryUser = new User("secretaria@ellosaude.com", passwordHash, tenantId, "Secretaria");
            context.Users.Add(secretaryUser);

            // Add sample patient
            var samplePatient = new Patient("Carlos Pereira", "carlos@email.com", "111.222.333-44", new DateTime(1985, 5, 15), tenantId);
            context.Patients.Add(samplePatient);

            await context.SaveChangesAsync();

            // Add sample appointment for patient
            var appointment = new Appointment(
                DateTime.UtcNow.Date.AddHours(10),
                DateTime.UtcNow.Date.AddHours(11),
                samplePatient.Id,
                doctorUser.Id,
                Domain.Enums.AppointmentType.Consulta,
                tenantId
            );
            context.Appointments.Add(appointment);

            await context.SaveChangesAsync();
        }
    }
}
