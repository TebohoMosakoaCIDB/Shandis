
using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class UserBlock : Entity
{
    public Guid BlockerId { get; private set; }
    public Guid BlockedUserId { get; private set; }

    private UserBlock() { }

    public UserBlock(Guid blockerId, Guid blockedUserId)
    {
        if (blockerId == Guid.Empty)
            throw new ArgumentException("Blocker ID is required.", nameof(blockerId));

        if (blockedUserId == Guid.Empty)
            throw new ArgumentException("Blocked user ID is required.", nameof(blockedUserId));

        if (blockerId == blockedUserId)
            throw new InvalidOperationException("Users cannot block themselves.");

        BlockerId = blockerId;
        BlockedUserId = blockedUserId;
    }
}
