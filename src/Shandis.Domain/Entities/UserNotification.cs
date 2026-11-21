using Shandis.Domain.Common;
using Shandis.Domain.Enums;

namespace Shandis.Domain.Entities;

public class UserNotification : Entity
{
    public Guid RecipientUserId { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string TargetUrl { get; private set; } = string.Empty;
    public Guid? MemoryId { get; private set; }
    public Guid? MomentId { get; private set; }
    public Guid? InvitationId { get; private set; }
    public bool IsResolved { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }

    private UserNotification() { }

    public UserNotification(
        Guid recipientUserId,
        Guid? actorUserId,
        NotificationType type,
        string title,
        string message,
        string targetUrl,
        Guid? memoryId = null,
        Guid? momentId = null,
        Guid? invitationId = null)
    {
        if (recipientUserId == Guid.Empty)
            throw new ArgumentException("A recipient user is required.", nameof(recipientUserId));
        if (actorUserId == Guid.Empty)
            throw new ArgumentException("Actor user ID cannot be an empty GUID.", nameof(actorUserId));
        if (!Enum.IsDefined(type))
            throw new ArgumentException("Notification type is invalid.", nameof(type));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Notification title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Notification message is required.", nameof(message));
        if (string.IsNullOrWhiteSpace(targetUrl) || !targetUrl.StartsWith('/'))
            throw new ArgumentException("A local notification target URL is required.", nameof(targetUrl));

        RecipientUserId = recipientUserId;
        ActorUserId = actorUserId;
        Type = type;
        Title = title.Trim();
        Message = message.Trim();
        TargetUrl = targetUrl.Trim();
        MemoryId = memoryId;
        MomentId = momentId;
        InvitationId = invitationId;
    }

    public void Resolve(string title, string message)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A notification title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("A notification message is required.", nameof(message));

        Title = title.Trim();
        Message = message.Trim();
        IsResolved = true;
        MarkAsRead();
        MarkAsUpdated();
    }

    public void MarkAsRead()
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAtUtc = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
