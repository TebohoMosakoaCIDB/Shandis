using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Notifications;

public sealed class NotificationItemResult
{
    public Guid Id { get; init; }
    public Guid? ActorUserId { get; init; }
    public string? ActorName { get; init; }
    public NotificationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string TargetUrl { get; init; } = string.Empty;
    public Guid? MemoryId { get; init; }
    public Guid? MomentId { get; init; }
    public Guid? InvitationId { get; init; }
    public bool IsResolved { get; init; }
    public bool IsRead { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ReadAtUtc { get; init; }
}
