using ElloSaude.Application.Common.Interfaces;
using ElloSaude.Application.Messaging;
using ElloSaude.Domain.Entities;
using ElloSaude.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Infrastructure.Messaging;

public sealed class WaitingRoomPatientArrivedHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IWaitingRoomNotifier _notifier;

    public WaitingRoomPatientArrivedHandler(
        IApplicationDbContext dbContext,
        IWaitingRoomNotifier notifier)
    {
        _dbContext = dbContext;
        _notifier = notifier;
    }

    public async Task HandleAsync(
        WaitingRoomPatientArrivedEvent notificationEvent,
        CancellationToken cancellationToken)
    {
        if (notificationEvent.EventId == Guid.Empty
            || notificationEvent.AppointmentId == Guid.Empty
            || notificationEvent.ProfessionalUserId == Guid.Empty
            || string.IsNullOrWhiteSpace(notificationEvent.TenantId))
            throw new InvalidOperationException("O evento de chegada não passou pela validação.");

        var appointment = await _dbContext.Appointments
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item =>
                item.Id == notificationEvent.AppointmentId
                && item.TenantId == notificationEvent.TenantId
                && !item.IsDeleted,
                cancellationToken);

        if (appointment is null
            || appointment.Status != AppointmentStatus.AguardandoRecepcao
            || !await _dbContext.Professionals
                .IgnoreQueryFilters()
                .AnyAsync(professional =>
                    professional.Id == appointment.ProfessionalId
                    && professional.UserId == notificationEvent.ProfessionalUserId
                    && professional.TenantId == notificationEvent.TenantId
                    && professional.IsActive
                    && !professional.IsDeleted,
                    cancellationToken))
            return;

        var notification = await _dbContext.Notifications
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(item =>
                item.EventId == notificationEvent.EventId
                && item.TenantId == notificationEvent.TenantId
                && item.RecipientUserId == notificationEvent.ProfessionalUserId,
                cancellationToken);

        if (notification?.RealtimeDeliveredAt is not null)
            return;

        if (notification is null)
        {
            notification = new InAppNotification(
                notificationEvent.EventId,
                notificationEvent.ProfessionalUserId,
                notificationEvent.AppointmentId,
                notificationEvent.ArrivedAt.UtcDateTime,
                notificationEvent.TenantId);
            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        await _notifier.NotifyPatientArrivedAsync(
            notificationEvent.ProfessionalUserId,
            notificationEvent.AppointmentId,
            notificationEvent.ArrivedAt,
            cancellationToken);

        notification.MarkRealtimeDelivered(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
