namespace Shandis.Application.Features.Notifications;

public sealed class NotificationFeedResult
{
    public IReadOnlyList<NotificationItemResult> Items { get; init; } = [];
    public int UnreadCount { get; init; }
}
