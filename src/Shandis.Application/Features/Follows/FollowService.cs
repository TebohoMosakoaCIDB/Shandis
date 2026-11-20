using Shandis.Application.Abstractions.Persistence;
using Shandis.Application.Features.Notifications;
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Follows;

public sealed class FollowService(
    IFollowRepository followRepository,
    INotificationService notificationService) : IFollowService
{
    public async Task<FollowRelationshipResult> FollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(followerId, followedUserId);

        if (followerId == followedUserId)
            throw new InvalidOperationException("You cannot follow yourself.");

        var canFollow = await followRepository.CanFollowAsync(
            followerId,
            followedUserId,
            cancellationToken);

        if (!canFollow)
            throw new KeyNotFoundException("This Shandis member could not be found.");

        var followerUserName = await followRepository.GetActiveUserNameAsync(
            followerId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(followerUserName))
            throw new UnauthorizedAccessException("Your Shandis account is not active.");

        var created = await followRepository.AddFollowAsync(
            followerId,
            followedUserId,
            cancellationToken);

        // Only create a notification when a new relationship was actually added.
        if (created)
        {
            await notificationService.CreateAsync(
                followedUserId,
                followerId,
                NotificationType.UserFollowed,
                "New follower",
                "You have a new follower.",
                $"/u/{Uri.EscapeDataString(followerUserName)}",
                cancellationToken: cancellationToken);
        }

        return new FollowRelationshipResult
        {
            UserId = followedUserId,
            IsFollowing = true
        };
    }

    public async Task<FollowRelationshipResult> UnfollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default)
    {
        ValidateIds(followerId, followedUserId);

        if (followerId == followedUserId)
            throw new InvalidOperationException("You cannot unfollow yourself.");

        var followedUserName = await followRepository.GetActiveUserNameAsync(
            followedUserId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(followedUserName))
            throw new KeyNotFoundException("This Shandis member could not be found.");

        await followRepository.RemoveFollowAsync(
            followerId,
            followedUserId,
            cancellationToken);

        return new FollowRelationshipResult
        {
            UserId = followedUserId,
            IsFollowing = false
        };
    }

    public async Task<FollowUsersResult> GetFollowersAsync(
        Guid userId,
        Guid currentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await GetUsersAsync(userId, currentUserId, page, pageSize, followers: true, cancellationToken);
    }

    public async Task<FollowUsersResult> GetFollowingAsync(
        Guid userId,
        Guid currentUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return await GetUsersAsync(userId, currentUserId, page, pageSize, followers: false, cancellationToken);
    }

    private async Task<FollowUsersResult> GetUsersAsync(
        Guid userId,
        Guid currentUserId,
        int page,
        int pageSize,
        bool followers,
        CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty || currentUserId == Guid.Empty)
            throw new ArgumentException("Valid user IDs are required.");

        var profileUserName = await followRepository.GetActiveUserNameAsync(userId, cancellationToken);

        if (string.IsNullOrWhiteSpace(profileUserName))
            throw new KeyNotFoundException("This Shandis member could not be found.");

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        return followers
            ? await followRepository.GetFollowersAsync(userId, currentUserId, page, pageSize, cancellationToken)
            : await followRepository.GetFollowingAsync(userId, currentUserId, page, pageSize, cancellationToken);
    }

    private static void ValidateIds(Guid followerId, Guid followedUserId)
    {
        if (followerId == Guid.Empty || followedUserId == Guid.Empty)
            throw new ArgumentException("Valid user IDs are required.");
    }
}
