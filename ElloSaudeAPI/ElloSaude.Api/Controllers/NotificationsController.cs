using ElloSaude.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ElloSaude.Api.Controllers;

[Authorize(Roles = "Profissional")]
[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IUserService _userService;

    public NotificationsController(IApplicationDbContext dbContext, IUserService userService)
    {
        _dbContext = dbContext;
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecent(
        [FromQuery] bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
        var userId = _userService.GetUserId();
        if (userId == Guid.Empty)
            return Forbid();

        var query = _dbContext.Notifications
            .Where(notification => notification.RecipientUserId == userId);

        if (unreadOnly)
            query = query.Where(notification => notification.ReadAt == null);

        var notifications = await query
            .OrderByDescending(notification => notification.OccurredAt)
            .Take(50)
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.Type,
                notification.Title,
                notification.Message,
                notification.AppointmentId,
                notification.OccurredAt,
                notification.ReadAt))
            .ToListAsync(cancellationToken);

        return Ok(notifications);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = _userService.GetUserId();
        if (userId == Guid.Empty)
            return Forbid();

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(
                item => item.Id == id && item.RecipientUserId == userId,
                cancellationToken);
        if (notification is null)
            return NotFound();

        notification.MarkRead(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private sealed record NotificationResponse(
        Guid Id,
        string Type,
        string Title,
        string Message,
        Guid AppointmentId,
        DateTime OccurredAt,
        DateTime? ReadAt);
}
