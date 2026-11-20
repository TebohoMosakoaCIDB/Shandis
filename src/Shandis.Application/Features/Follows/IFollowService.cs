namespace Shandis.Application.Features.Follows;

public interface IFollowService
{
    Task<FollowRelationshipResult> FollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default);

    Task<FollowRelationshipResult> UnfollowAsync(
        Guid followerId,
        Guid followedUserId,
        CancellationToken cancellationToken = default);
}
