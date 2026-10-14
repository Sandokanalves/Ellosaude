using ElloSaude.Application.Appointments.Commands;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using ElloSaude.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
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
        var notifier = new Mock<IWaitingRoomNotifier>();
        var handler = new UpdateAppointmentStatusHandler(context, userService.Object, notifier.Object);

        await handler.Handle(
            new UpdateAppointmentStatusCommand(appointment.Id, AppointmentStatus.AguardandoRecepcao),
            CancellationToken.None);

        appointment.Status.Should().Be(AppointmentStatus.AguardandoRecepcao);
        appointment.ArrivedAt.Should().NotBeNull();
        notifier.Verify(service => service.NotifyPatientArrivedAsync(
            professionalUserId,
            appointment.Id,
            It.Is<DateTimeOffset>(timestamp => timestamp.UtcDateTime == appointment.ArrivedAt),
            CancellationToken.None), Times.Once);

        await handler.Handle(
            new UpdateAppointmentStatusCommand(appointment.Id, AppointmentStatus.AguardandoRecepcao),
            CancellationToken.None);

        notifier.Verify(service => service.NotifyPatientArrivedAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
