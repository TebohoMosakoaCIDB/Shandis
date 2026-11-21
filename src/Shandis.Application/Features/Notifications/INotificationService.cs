using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Notifications;

public interface INotificationService
{
    Task CreateAsync(
        Guid recipientUserId,
        Guid? actorUserId,
        NotificationType type,
        string title,
        string message,
        string targetUrl,
        Guid? memoryId = null,
        Guid? momentId = null,
        CancellationToken cancellationToken = default,
        Guid? invitationId = null);

    Task<int> ResolveInvitationAsync(Guid invitationId, Guid recipientUserId, string title, string message, CancellationToken cancellationToken = default);

    Task<NotificationFeedResult> GetForUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
