using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.Messaging;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using ElloSaude.Infrastructure.Messaging;
using ElloSaude.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ElloSaude.UnitTests.Infrastructure;

public class WaitingRoomPatientArrivedHandlerTests
{
    [Fact]
    public async Task HandleAsync_PersistsAndNotifiesOnce_WhenEventIsRedelivered()
    {
        const string tenantId = "tenant-a";
        var professionalUserId = Guid.NewGuid();
        var (professional, appointment) = CreateWaitingAppointment(tenantId, professionalUserId);
        var notifier = new Mock<IWaitingRoomNotifier>();
        await using var context = CreateContext(tenantId, professional, appointment);
        var handler = new WaitingRoomPatientArrivedHandler(context, notifier.Object);
        var notificationEvent = CreateEvent(tenantId, professionalUserId, appointment.Id);

        await handler.HandleAsync(notificationEvent, CancellationToken.None);
        await handler.HandleAsync(notificationEvent, CancellationToken.None);

        var persistedNotification = await context.Notifications.SingleAsync();
        Assert.Equal(notificationEvent.EventId, persistedNotification.EventId);
        Assert.Equal(professionalUserId, persistedNotification.RecipientUserId);
        Assert.NotNull(persistedNotification.RealtimeDeliveredAt);
        notifier.Verify(
            service => service.NotifyPatientArrivedAsync(
                professionalUserId,
                appointment.Id,
                notificationEvent.ArrivedAt,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNotNotifyWhenAppointmentIsNoLongerWaiting()
    {
        const string tenantId = "tenant-a";
        var professionalUserId = Guid.NewGuid();
        var (professional, appointment) = CreateWaitingAppointment(tenantId, professionalUserId);
        appointment.UpdateStatus(AppointmentStatus.EmAtendimento);
        var notifier = new Mock<IWaitingRoomNotifier>();
        await using var context = CreateContext(tenantId, professional, appointment);
        var handler = new WaitingRoomPatientArrivedHandler(context, notifier.Object);

        await handler.HandleAsync(
            CreateEvent(tenantId, professionalUserId, appointment.Id),
            CancellationToken.None);

        Assert.Empty(await context.Notifications.ToListAsync());
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_DoesNotCrossTenantWhenEventTenantDoesNotMatchAppointment()
    {
        const string tenantId = "tenant-a";
        var professionalUserId = Guid.NewGuid();
        var (professional, appointment) = CreateWaitingAppointment(tenantId, professionalUserId);
        var notifier = new Mock<IWaitingRoomNotifier>();
        await using var context = CreateContext(tenantId, professional, appointment);
        var handler = new WaitingRoomPatientArrivedHandler(context, notifier.Object);

        await handler.HandleAsync(
            CreateEvent("tenant-b", professionalUserId, appointment.Id),
            CancellationToken.None);

        Assert.Empty(await context.Notifications.ToListAsync());
        notifier.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_RetriesRealtimeDelivery_WhenInitialPushFailsAfterPersistence()
    {
        const string tenantId = "tenant-a";
        var professionalUserId = Guid.NewGuid();
        var (professional, appointment) = CreateWaitingAppointment(tenantId, professionalUserId);
        var notifier = new Mock<IWaitingRoomNotifier>();
        notifier.SetupSequence(service => service.NotifyPatientArrivedAsync(
                professionalUserId,
                appointment.Id,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transient SignalR failure."))
            .Returns(Task.CompletedTask);
        await using var context = CreateContext(tenantId, professional, appointment);
        var handler = new WaitingRoomPatientArrivedHandler(context, notifier.Object);
        var notificationEvent = CreateEvent(tenantId, professionalUserId, appointment.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(notificationEvent, CancellationToken.None));
        Assert.Null((await context.Notifications.SingleAsync()).RealtimeDeliveredAt);

        await handler.HandleAsync(notificationEvent, CancellationToken.None);

        Assert.NotNull((await context.Notifications.SingleAsync()).RealtimeDeliveredAt);
        notifier.Verify(
            service => service.NotifyPatientArrivedAsync(
                professionalUserId,
                appointment.Id,
                notificationEvent.ArrivedAt,
                CancellationToken.None),
            Times.Exactly(2));
    }

    private static ApplicationDbContext CreateContext(
        string tenantId,
        Professional professional,
        Appointment appointment)
    {
        var tenantService = new Mock<ITenantService>();
        tenantService.Setup(service => service.GetTenantId()).Returns(tenantId);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options, tenantService.Object);
        context.Professionals.Add(professional);
        context.Patients.Add(new Patient(
            "Paciente",
            "paciente@example.test",
            Guid.NewGuid().ToString("N"),
            DateTime.UtcNow.AddYears(-30),
            tenantId));
        context.Appointments.Add(appointment);
        context.SaveChanges();
        return context;
    }

    private static (Professional Professional, Appointment Appointment) CreateWaitingAppointment(
        string tenantId,
        Guid professionalUserId)
    {
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
            Guid.NewGuid().ToString("N"),
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
        appointment.MarkAsWaiting();
        return (professional, appointment);
    }

    private static WaitingRoomPatientArrivedEvent CreateEvent(
        string tenantId,
        Guid professionalUserId,
        Guid appointmentId) =>
        new(Guid.NewGuid(), appointmentId, professionalUserId, DateTimeOffset.UtcNow, tenantId);
}
