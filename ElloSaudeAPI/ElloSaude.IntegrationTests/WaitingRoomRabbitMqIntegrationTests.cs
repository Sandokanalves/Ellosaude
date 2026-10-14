using System.Text.Json;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.Messaging;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using ElloSaude.Infrastructure.Messaging;
using ElloSaude.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ElloSaude.IntegrationTests;

public sealed class WaitingRoomRabbitMqIntegrationTests
{
    [RabbitMqFact]
    [Trait("Category", "RabbitMq")]
    public async Task OutboxEvent_IsPublishedConsumedPersistedAndPushed()
    {
        var tenantId = $"tenant-{Guid.NewGuid():N}";
        var professionalUserId = Guid.NewGuid();
        var professional = new Professional("Profissional", "123", "SP", "Clínica", tenantId, professionalUserId);
        var patient = new Patient(
            "Paciente",
            $"patient-{Guid.NewGuid():N}@example.test",
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

        var notificationEvent = new WaitingRoomPatientArrivedEvent(
            Guid.NewGuid(),
            appointment.Id,
            professionalUserId,
            new DateTimeOffset(DateTime.SpecifyKind(appointment.ArrivedAt!.Value, DateTimeKind.Utc)),
            tenantId);
        var notifier = new TestWaitingRoomNotifier();
        var databaseName = $"RabbitMqTest-{Guid.NewGuid():N}";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMq:Host"] = Environment.GetEnvironmentVariable("RabbitMq__Host") ?? "localhost",
                ["RabbitMq:Port"] = Environment.GetEnvironmentVariable("RabbitMq__Port") ?? "5672",
                ["RabbitMq:UserName"] = Environment.GetEnvironmentVariable("RabbitMq__UserName") ?? "guest",
                ["RabbitMq:Password"] = Environment.GetEnvironmentVariable("RabbitMq__Password") ?? "guest"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        services.AddSingleton<ITenantService>(new TestTenantService(tenantId));
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());
        services.AddSingleton<IWaitingRoomNotifier>(notifier);
        services.AddScoped<WaitingRoomPatientArrivedHandler>();
        services.AddSingleton(new RabbitMqConnectionProvider(configuration));
        services.AddSingleton<IMessageBusService, RabbitMqService>();
        await using var serviceProvider = services.BuildServiceProvider();

        await using (var scope = serviceProvider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.AddRange(professional, patient, appointment);
            dbContext.OutboxMessages.Add(new OutboxMessage(
                WaitingRoomQueues.PatientArrived,
                nameof(WaitingRoomPatientArrivedEvent),
                tenantId,
                JsonSerializer.Serialize(notificationEvent)));
            await dbContext.SaveChangesAsync();
        }

        var consumer = new WaitingRoomNotificationConsumerService(
            serviceProvider.GetRequiredService<RabbitMqConnectionProvider>(),
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<ILogger<WaitingRoomNotificationConsumerService>>());
        var publisher = new OutboxPublisherService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            serviceProvider.GetRequiredService<IMessageBusService>(),
            serviceProvider.GetRequiredService<ILogger<OutboxPublisherService>>());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));

        try
        {
            await consumer.StartAsync(timeout.Token);
            await publisher.StartAsync(timeout.Token);

            InAppNotification? persistedNotification = null;
            OutboxMessage? publishedOutboxMessage = null;
            while ((persistedNotification?.RealtimeDeliveredAt is null
                    || publishedOutboxMessage?.PublishedAt is null)
                && !timeout.IsCancellationRequested)
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                persistedNotification = await dbContext.Notifications
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(item => item.EventId == notificationEvent.EventId);
                publishedOutboxMessage = await dbContext.OutboxMessages
                    .FirstOrDefaultAsync();
                if (persistedNotification?.RealtimeDeliveredAt is null
                    || publishedOutboxMessage?.PublishedAt is null)
                    await Task.Delay(250, timeout.Token);
            }

            Assert.NotNull(persistedNotification);
            Assert.NotNull(persistedNotification!.RealtimeDeliveredAt);
            Assert.Equal(professionalUserId, persistedNotification.RecipientUserId);
            var deliveredEvent = await notifier.Arrival.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(professionalUserId, deliveredEvent.ProfessionalUserId);
            Assert.Equal(appointment.Id, deliveredEvent.AppointmentId);
            Assert.Equal(notificationEvent.ArrivedAt, deliveredEvent.ArrivedAt);
            Assert.Equal(2, notifier.DeliveryCount);

            Assert.NotNull(publishedOutboxMessage!.PublishedAt);
        }
        finally
        {
            using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await publisher.StopAsync(stopTimeout.Token);
            await consumer.StopAsync(stopTimeout.Token);
        }
    }

    private sealed class TestTenantService(string tenantId) : ITenantService
    {
        public string GetTenantId() => tenantId;
        public Guid GetUserId() => Guid.Empty;
    }

    private sealed class TestWaitingRoomNotifier : IWaitingRoomNotifier
    {
        public TaskCompletionSource<WaitingRoomPatientArrivedEvent> Arrival { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _deliveryCount;
        public int DeliveryCount => Volatile.Read(ref _deliveryCount);

        public Task NotifyPatientArrivedAsync(
            Guid professionalUserId,
            Guid appointmentId,
            DateTimeOffset arrivedAt,
            CancellationToken cancellationToken)
        {
            var attempt = Interlocked.Increment(ref _deliveryCount);
            if (attempt == 1)
                return Task.FromException(new InvalidOperationException("Transient SignalR failure."));

            Arrival.TrySetResult(new(
                Guid.NewGuid(),
                appointmentId,
                professionalUserId,
                arrivedAt,
                string.Empty));
            return Task.CompletedTask;
        }
    }
}
