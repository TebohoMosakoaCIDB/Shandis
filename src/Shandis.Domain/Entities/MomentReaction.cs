
using Shandis.Domain.Common;
using Shandis.Domain.Enums;

namespace Shandis.Domain.Entities;

public class MomentReaction : Entity
{
    public Guid MomentId { get; private set; }
    public Guid UserId { get; private set; }
    public ReactionType ReactionType { get; private set; }

    public MemoryMoment Moment { get; private set; } = null!;

    private MomentReaction() { }

    public MomentReaction(Guid momentId, Guid userId, ReactionType reactionType)
    {
        if (momentId == Guid.Empty)
            throw new ArgumentException("Moment ID is required.", nameof(momentId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        MomentId = momentId;
        UserId = userId;
        ReactionType = reactionType;
    }

    public void ChangeReaction(ReactionType reactionType)
    {
        ReactionType = reactionType;
        MarkAsUpdated();
    }
}