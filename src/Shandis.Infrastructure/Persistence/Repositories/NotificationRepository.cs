using Microsoft.EntityFrameworkCore;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Application.Features.Notifications;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Persistence;

namespace Shandis.Infrastructure.Persistence.Repositories;

public sealed class NotificationRepository(IDbContextFactory<ShandisDbContext> contextFactory)
    : INotificationRepository
{
    public async Task AddAsync(UserNotification notification, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Set<UserNotification>().Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationItemResult>> GetForUserAsync(
        Guid userId, int take = 30, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var query =
            from notification in dbContext.Set<UserNotification>().AsNoTracking()
            join actor in dbContext.Users.AsNoTracking()
                on notification.ActorUserId equals actor.Id into actorGroup
            from actor in actorGroup.DefaultIfEmpty()
            where notification.RecipientUserId == userId
            orderby notification.CreatedAtUtc descending
            select new NotificationItemResult
            {
                Id = notification.Id,
                ActorUserId = notification.ActorUserId,
                ActorName = actor == null
                    ? null
                    : (actor.DisplayName != null && actor.DisplayName != ""
                        ? actor.DisplayName
                        : actor.FirstName + " " + actor.LastName),
                Type = notification.Type,
                Title = notification.Title,
                Message = notification.Message,
                TargetUrl = notification.TargetUrl,
                MemoryId = notification.MemoryId,
                MomentId = notification.MomentId,
                IsRead = notification.IsRead,
                CreatedAtUtc = notification.CreatedAtUtc,
                ReadAtUtc = notification.ReadAtUtc
            };

        return await query.Take(Math.Clamp(take, 1, 100)).ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Set<UserNotification>()
            .CountAsync(x => x.RecipientUserId == userId && !x.IsRead, cancellationToken);
    }

    public async Task<bool> MarkReadAsync(
        Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var notification = await dbContext.Set<UserNotification>()
            .FirstOrDefaultAsync(
                x => x.Id == notificationId && x.RecipientUserId == userId,
                cancellationToken);

        if (notification is null)
            return false;

        notification.MarkAsRead();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        var notifications = await dbContext.Set<UserNotification>()
            .Where(x => x.RecipientUserId == userId && !x.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
            notification.MarkAsRead();

        await dbContext.SaveChangesAsync(cancellationToken);
        return notifications.Count;
    }
}
