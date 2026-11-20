using Microsoft.EntityFrameworkCore;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Application.Features.Follows;
using Shandis.Domain.Entities;
using Shandis.Infrastructure.Identity;

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
            .AnyAsync(user => user.Id == followedUserId && user.IsActive, cancellationToken);

        if (!followedUserIsActive)
            return false;

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
                follow => follow.FollowerId == followerId && follow.FollowedUserId == followedUserId,
                cancellationToken);
    }

    public async Task<bool> AddFollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

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
            .Where(follow => follow.FollowerId == followerId && follow.FollowedUserId == followedUserId)
            .ExecuteDeleteAsync(cancellationToken);

        return affectedRows > 0;
    }

    public async Task<int> GetFollowerCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.UserFollows
            .AsNoTracking()
            .Where(follow => follow.FollowedUserId == userId)
            .Join(dbContext.Users, follow => follow.FollowerId, user => user.Id, (_, user) => user)
            .CountAsync(user => user.IsActive, cancellationToken);
    }

    public async Task<int> GetFollowingCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.UserFollows
            .AsNoTracking()
            .Where(follow => follow.FollowerId == userId)
            .Join(dbContext.Users, follow => follow.FollowedUserId, user => user.Id, (_, user) => user)
            .CountAsync(user => user.IsActive, cancellationToken);
    }

    public Task<FollowUsersResult> GetFollowersAsync(
        Guid userId,
        Guid currentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return GetUsersAsync(userId, currentUserId, page, pageSize, followers: true, cancellationToken);
    }

    public Task<FollowUsersResult> GetFollowingAsync(
        Guid userId,
        Guid currentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return GetUsersAsync(userId, currentUserId, page, pageSize, followers: false, cancellationToken);
    }

    private async Task<FollowUsersResult> GetUsersAsync(
        Guid userId,
        Guid currentUserId,
        int page,
        int pageSize,
        bool followers,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<ShandisUser> users = followers
            ? dbContext.UserFollows
                .AsNoTracking()
                .Where(follow => follow.FollowedUserId == userId)
                .Join(dbContext.Users, follow => follow.FollowerId, user => user.Id, (_, user) => user)
            : dbContext.UserFollows
                .AsNoTracking()
                .Where(follow => follow.FollowerId == userId)
                .Join(dbContext.Users, follow => follow.FollowedUserId, user => user.Id, (_, user) => user);

        users = users.Where(user => user.IsActive);

        var totalCount = await users.CountAsync(cancellationToken);

        var items = await users
            .OrderBy(user => user.DisplayName ?? user.UserName)
            .ThenBy(user => user.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new FollowUserItem
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                DisplayName = user.DisplayName ?? user.UserName ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsFollowing = dbContext.UserFollows.Any(
                    follow => follow.FollowerId == currentUserId &&
                              follow.FollowedUserId == user.Id)
            })
            .ToListAsync(cancellationToken);

        return new FollowUsersResult
        {
            UserId = userId,
            Relationship = followers ? "Followers" : "Following",
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Users = items
        };
    }
}
