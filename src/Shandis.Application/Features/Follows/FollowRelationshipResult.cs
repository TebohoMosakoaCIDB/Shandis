namespace Shandis.Application.Features.Follows;

public sealed class FollowRelationshipResult
{
    public Guid UserId { get; init; }
    public bool IsFollowing { get; init; }
}
