using System.Text.Json;
using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.Messaging;
using ElloSaude.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElloSaude.Infrastructure.Messaging;

public sealed class OutboxPublisherService : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessageBusService _messageBus;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IServiceScopeFactory scopeFactory,
        IMessageBusService messageBus,
        ILogger<OutboxPublisherService> logger)
    {
        _scopeFactory = scopeFactory;
        _messageBus = messageBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await PublishPendingBatchAsync(stoppingToken);
                if (!processed)
                    await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Falha ao processar a outbox de mensagens.");
                await Task.Delay(PollingInterval, stoppingToken);
            }
        }
    }

    private async Task<bool> PublishPendingBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var now = DateTime.UtcNow;
        var pending = await dbContext.OutboxMessages
            .Where(message => message.PublishedAt == null
                && (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                await PublishMessageAsync(message, cancellationToken);
                message.MarkPublished(DateTime.UtcNow);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.RegisterFailure($"{exception.GetType().Name}: {exception.Message}");
                _logger.LogError(
                    exception,
                    "Falha ao publicar mensagem de outbox {OutboxMessageId}; nova tentativa agendada.",
                    message.Id);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return pending.Count > 0;
    }

    private Task PublishMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (message.EventType != nameof(WaitingRoomPatientArrivedEvent)
            || message.Queue != WaitingRoomQueues.PatientArrived)
            throw new InvalidOperationException(
                $"Tipo de evento de outbox não suportado: {message.EventType}.");

        var notificationEvent = JsonSerializer.Deserialize<WaitingRoomPatientArrivedEvent>(message.Payload)
            ?? throw new JsonException("O evento da outbox não contém um payload válido.");

        if (notificationEvent.TenantId != message.TenantId
            || notificationEvent.EventId == Guid.Empty
            || notificationEvent.AppointmentId == Guid.Empty
            || notificationEvent.ProfessionalUserId == Guid.Empty)
            throw new InvalidOperationException("O payload do evento de outbox não passou pela validação.");

        return _messageBus.PublishAsync(message.Queue, notificationEvent, cancellationToken);
    }
}
