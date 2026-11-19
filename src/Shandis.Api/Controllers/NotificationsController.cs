using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shandis.Application.Features.Notifications;

namespace Shandis.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationFeedResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        return Ok(await notificationService.GetForUserAsync(userId, cancellationToken));
    }

    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var updated = await notificationService.MarkReadAsync(notificationId, userId, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var count = await notificationService.MarkAllReadAsync(userId, cancellationToken);
        return Ok(new { markedRead = count });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("sub");
        return Guid.TryParse(value, out userId);
    }
}
