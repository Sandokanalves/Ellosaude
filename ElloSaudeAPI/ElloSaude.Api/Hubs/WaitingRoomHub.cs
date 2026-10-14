using System.Security.Claims;
using ElloSaude.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ElloSaude.Api.Hubs;

[Authorize(Roles = "Profissional")]
public sealed class WaitingRoomHub : Hub
{
}

public sealed class SignalRWaitingRoomNotifier : IWaitingRoomNotifier
{
    private readonly IHubContext<WaitingRoomHub> _hubContext;

    public SignalRWaitingRoomNotifier(IHubContext<WaitingRoomHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyPatientArrivedAsync(
        Guid professionalUserId,
        Guid appointmentId,
        DateTimeOffset arrivedAt,
        CancellationToken cancellationToken)
    {
        var notification = new PatientArrivedNotification(appointmentId, arrivedAt);
        return _hubContext.Clients
            .User(professionalUserId.ToString())
            .SendAsync("PatientArrived", notification, cancellationToken);
    }
}

/// <summary>Notification sent to the assigned professional when a patient arrives.</summary>
public sealed record PatientArrivedNotification(Guid AppointmentId, DateTimeOffset ArrivedAt);
