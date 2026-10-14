using ElloSaude.Application.Appointments.Commands;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.Messaging;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using ElloSaude.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Text.Json;
using Xunit;

namespace ElloSaude.UnitTests.Application;

public class AppointmentStatusNotificationTests
{
    [Fact]
    public async Task MarkingAppointmentAsWaiting_ShouldNotifyLinkedProfessional()
    {
        var tenantId = "tenant-a";
        var professionalUserId = Guid.NewGuid();
        var tenantService = new Mock<ITenantService>();
        tenantService.Setup(service => service.GetTenantId()).Returns(tenantId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, tenantService.Object);

        var professional = new Professional(
            "Profissional",
            "123",
            "SP",
            "Clínica",
            tenantId,
            professionalUserId);
        var patient = new Patient(
            "Paciente",
            "paciente@example.test",
            "123",
            DateTime.UtcNow.AddYears(-30),
            tenantId);
        var appointment = new Appointment(
            DateTime.UtcNow.AddMinutes(-10),
            DateTime.UtcNow.AddMinutes(20),
            patient.Id,
            professional.Id,
            AppointmentType.Consulta,
            tenantId);
        appointment.Confirm();
        context.AddRange(professional, patient, appointment);
        await context.SaveChangesAsync();

        var userService = new Mock<IUserService>();
        userService.Setup(service => service.GetUserRole()).Returns("Secretaria");
        userService.Setup(service => service.GetUserId()).Returns(Guid.NewGuid());
        var handler = new UpdateAppointmentStatusHandler(context, userService.Object);

        await handler.Handle(
            new UpdateAppointmentStatusCommand(appointment.Id, AppointmentStatus.AguardandoRecepcao),
            CancellationToken.None);

        appointment.Status.Should().Be(AppointmentStatus.AguardandoRecepcao);
        appointment.ArrivedAt.Should().NotBeNull();
        var outboxMessages = await context.OutboxMessages.ToListAsync();
        outboxMessages.Should().ContainSingle();
        outboxMessages[0].Queue.Should().Be(WaitingRoomQueues.PatientArrived);
        outboxMessages[0].TenantId.Should().Be(tenantId);

        await handler.Handle(
            new UpdateAppointmentStatusCommand(appointment.Id, AppointmentStatus.AguardandoRecepcao),
            CancellationToken.None);

        (await context.OutboxMessages.CountAsync()).Should().Be(1);

        var notificationEvent = JsonSerializer.Deserialize<WaitingRoomPatientArrivedEvent>(
            outboxMessages[0].Payload);
        notificationEvent.Should().NotBeNull();
        notificationEvent!.ProfessionalUserId.Should().Be(professionalUserId);
        notificationEvent.AppointmentId.Should().Be(appointment.Id);
        notificationEvent.TenantId.Should().Be(tenantId);
    }
}
