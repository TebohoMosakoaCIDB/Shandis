using Shandis.Application.Features.Follows;

namespace Shandis.Application.Abstractions.Persistence;

public interface IFollowRepository
{
    Task<bool> CanFollowAsync(Guid followerId, Guid followedUserId, CancellationToken cancellationToken = default);
    Task<string?> GetActiveUserNameAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsFollowingAsync(Guid followerId, Guid followedUserId, CancellationToken cancellationToken = default);
    Task<bool> AddFollowAsync(Guid followerId, Guid followedUserId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFollowAsync(Guid followerId, Guid followedUserId, CancellationToken cancellationToken = default);
    Task<int> GetFollowerCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<int> GetFollowingCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<FollowUsersResult> GetFollowersAsync(Guid userId, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<FollowUsersResult> GetFollowingAsync(Guid userId, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken = default);
}