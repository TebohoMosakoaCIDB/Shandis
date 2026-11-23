using Shandis.Application.Features.Notifications;
using Shandis.Domain.Entities;

namespace Shandis.Application.Abstractions.Persistence;

public interface INotificationRepository
{
    Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default);
    Task<int> ResolveInvitationAsync(Guid invitationId, Guid recipientUserId, string title, string message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationItemResult>> GetForUserAsync(Guid userId, int take = 30, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default);
}
