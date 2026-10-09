using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IHashService hashService,
        string seedPassword)
    {
        if (string.IsNullOrWhiteSpace(seedPassword) || seedPassword.Length < 16)
            throw new ArgumentException("A senha de seed deve ter ao menos 16 caracteres.", nameof(seedPassword));

        if (!await context.Clinics.IgnoreQueryFilters().AnyAsync())
        {
            var tenantId = Guid.NewGuid().ToString();
            var clinic = new Clinic("Clínica ElloSaúde Matriz", "12.345.678/0001-90", tenantId);
            context.Clinics.Add(clinic);

            var passwordHash = hashService.HashPassword(seedPassword);

            // 1. Usuário Administrador da Clínica
            var adminUser = new User("admin@ellosaude.com", passwordHash, tenantId, "Admin");
            context.Users.Add(adminUser);

            // 2. Usuário Médico e Entidade Professional correspondente
            var doctorUser = new User("doutor@ellosaude.com", passwordHash, tenantId, "Profissional");
            context.Users.Add(doctorUser);

            var professional = new Professional(
                name: "Dr. João Silva",
                crm: "123456",
                crmState: "PE",
                specialty: "Clínica Geral",
                tenantId: tenantId,
                userId: doctorUser.Id,
                phone: "(81) 98888-7777",
                email: "doutor@ellosaude.com",
                consultationPrice: 200.00m
            );
            context.Professionals.Add(professional);

            // Configurar disponibilidade padrão de segunda a sexta (08:00 às 18:00 com pausa das 12:00 às 13:00)
            var weekDays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
            foreach (var day in weekDays)
            {
                var availability = new DoctorAvailability(
                    professionalId: professional.Id,
                    dayOfWeek: day,
                    startTime: new TimeSpan(8, 0, 0),
                    endTime: new TimeSpan(18, 0, 0),
                    tenantId: tenantId,
                    breakStartTime: new TimeSpan(12, 0, 0),
                    breakEndTime: new TimeSpan(13, 0, 0),
                    slotDurationMinutes: 30
                );
                context.DoctorAvailabilities.Add(availability);
            }

            // 3. Usuário Secretária
            var secretaryUser = new User("secretaria@ellosaude.com", passwordHash, tenantId, "Secretaria");
            context.Users.Add(secretaryUser);

            // 4. Pacientes de Exemplo
            var samplePatient = new Patient(
                name: "Carlos Pereira",
                email: "carlos@email.com",
                cpf: "111.222.333-44",
                birthDate: new DateTime(1985, 5, 15),
                tenantId: tenantId,
                phone: "(81) 99999-1111",
                address: "Av. Boa Viagem, 1000 - Recife/PE",
                emergencyContact: "Maria Pereira (81) 99999-2222",
                gender: "Masculino"
            );
            var patientUser = new User("carlos@email.com", passwordHash, tenantId, "Paciente");
            samplePatient.LinkPortalAccount(patientUser);
            context.Users.Add(patientUser);
            context.Patients.Add(samplePatient);

            var samplePatient2 = new Patient(
                name: "Ana Souza",
                email: "ana@email.com",
                cpf: "222.333.444-55",
                birthDate: new DateTime(1990, 8, 20),
                tenantId: tenantId,
                phone: "(81) 98765-4321",
                gender: "Feminino"
            );
            context.Patients.Add(samplePatient2);

            await context.SaveChangesAsync();

            // 5. Agendamento de Exemplo e Registro Financeiro
            var appointmentStart = DateTime.UtcNow.Date.AddDays(1).AddHours(10);
            var appointmentEnd = appointmentStart.AddMinutes(30);

            var appointment = new Appointment(
                start: appointmentStart,
                end: appointmentEnd,
                patientId: samplePatient.Id,
                professionalId: professional.Id,
                type: Domain.Enums.AppointmentType.Consulta,
                tenantId: tenantId,
                price: 200.00m,
                isFreeReturn: false,
                observations: "Primeira consulta de rotina"
            );
            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();

            // Lançamento financeiro real para o agendamento
            var payment = new PaymentRecord(
                appointmentId: appointment.Id,
                patientId: samplePatient.Id,
                professionalId: professional.Id,
                expectedAmount: 200.00m,
                tenantId: tenantId,
                registeredByUserId: adminUser.Id,
                isFreeReturn: false,
                notes: "Aguardando pagamento na recepção"
            );
            context.PaymentRecords.Add(payment);

            await context.SaveChangesAsync();
        }
    }
}
