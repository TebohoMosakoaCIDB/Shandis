namespace Shandis.Application.Abstractions.Persistence;

public interface IFollowRepository
{
    Task<bool> CanFollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default);

    Task<string?> GetActiveUserNameAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsFollowingAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default);

    Task<bool> AddFollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveFollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default);
}
