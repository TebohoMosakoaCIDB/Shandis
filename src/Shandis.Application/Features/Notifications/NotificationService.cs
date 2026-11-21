using Shandis.Application.Abstractions.Persistence;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Notifications;

public sealed class NotificationService(INotificationRepository repository) : INotificationService
{
    public async Task CreateAsync(
        Guid recipientUserId,
        Guid? actorUserId,
        NotificationType type,
        string title,
        string message,
        string targetUrl,
        Guid? memoryId = null,
        Guid? momentId = null,
        CancellationToken cancellationToken = default,
        Guid? invitationId = null)
    {
        if (recipientUserId == Guid.Empty)
            throw new ArgumentException("A valid notification recipient is required.", nameof(recipientUserId));

        var notification = new UserNotification(
            recipientUserId,
            actorUserId,
            type,
            title,
            message,
            targetUrl,
            memoryId,
            momentId,
            invitationId);

        await repository.AddAsync(notification, cancellationToken);
    }

    public Task<int> ResolveInvitationAsync(Guid invitationId, Guid recipientUserId, string title, string message, CancellationToken cancellationToken = default)
    {
        if (invitationId == Guid.Empty || recipientUserId == Guid.Empty)
            throw new ArgumentException("Valid invitation and recipient IDs are required.");

        return repository.ResolveInvitationAsync(invitationId, recipientUserId, title, message, cancellationToken);
    }

    public async Task<NotificationFeedResult> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));

        var items = await repository.GetForUserAsync(userId, 30, cancellationToken);
        var unreadCount = await repository.GetUnreadCountAsync(userId, cancellationToken);

        return new NotificationFeedResult
        {
            Items = items,
            UnreadCount = unreadCount
        };
    }

    public Task<bool> MarkReadAsync(
        Guid notificationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (notificationId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Valid notification and user IDs are required.");

        return repository.MarkReadAsync(notificationId, userId, cancellationToken);
    }

    public Task<int> MarkAllReadAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));

        return repository.MarkAllReadAsync(userId, cancellationToken);
    }
}
