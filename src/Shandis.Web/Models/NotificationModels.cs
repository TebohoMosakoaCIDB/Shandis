namespace Shandis.Web.Models;

public sealed class NotificationFeed
{
    public List<NotificationItem> Items { get; set; } = [];
    public int UnreadCount { get; set; }
}

public sealed class NotificationItem
{
    public Guid Id { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorName { get; set; }
    public int Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = "/";
    public Guid? MemoryId { get; set; }
    public Guid? MomentId { get; set; }
    public Guid? InvitationId { get; set; }
    public bool IsResolved { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
