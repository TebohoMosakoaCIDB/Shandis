using Microsoft.EntityFrameworkCore;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Domain.Entities;

namespace Shandis.Infrastructure.Persistence.Repositories;

public sealed class FollowRepository(IDbContextFactory<ShandisDbContext> contextFactory)
    : IFollowRepository
{
    public async Task<bool> CanFollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var followedUserIsActive = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == followedUserId && user.IsActive,
                cancellationToken);

        if (!followedUserIsActive)
            return false;

        // A block in either direction prevents a follow relationship.
        var isBlocked = await dbContext.UserBlocks
            .AsNoTracking()
            .AnyAsync(
                block =>
                    (block.BlockerId == followerId && block.BlockedUserId == followedUserId) ||
                    (block.BlockerId == followedUserId && block.BlockedUserId == followerId),
                cancellationToken);

        return !isBlocked;
    }

    public async Task<string?> GetActiveUserNameAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => user.UserName)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsFollowingAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.UserFollows
            .AsNoTracking()
            .AnyAsync(
                follow => follow.FollowerId == followerId &&
                          follow.FollowedUserId == followedUserId,
                cancellationToken);
    }

    public async Task<bool> AddFollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        // PostgreSQL's unique index makes this operation safe for duplicate requests,
        // including requests that arrive concurrently.
        var now = DateTime.UtcNow;
        var followId = Guid.NewGuid();

        var affectedRows = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "UserFollows" ("Id", "FollowerId", "FollowedUserId", "CreatedAtUtc")
             VALUES ({followId}, {followerId}, {followedUserId}, {now})
             ON CONFLICT ("FollowerId", "FollowedUserId") DO NOTHING
             """,
            cancellationToken);

        return affectedRows > 0;
    }

    public async Task<bool> RemoveFollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        var affectedRows = await dbContext.UserFollows
            .Where(follow =>
                follow.FollowerId == followerId &&
                follow.FollowedUserId == followedUserId)
            .ExecuteDeleteAsync(cancellationToken);

        return affectedRows > 0;
    }
}
