
using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class UserFollow : Entity
{
    public Guid FollowerId { get; private set; }
    public Guid FollowedUserId { get; private set; }

    private UserFollow() { }

    public UserFollow(Guid followerId, Guid followedUserId)
    {
        if (followerId == Guid.Empty)
            throw new ArgumentException("Follower ID is required.", nameof(followerId));

        if (followedUserId == Guid.Empty)
            throw new ArgumentException("Followed user ID is required.", nameof(followedUserId));

        if (followerId == followedUserId)
            throw new InvalidOperationException("Users cannot follow themselves.");

        FollowerId = followerId;
        FollowedUserId = followedUserId;
    }
}
