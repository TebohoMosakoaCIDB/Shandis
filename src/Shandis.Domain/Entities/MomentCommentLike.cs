using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class MomentCommentLike : Entity
{
    public Guid CommentId { get; private set; }
    public Guid UserId { get; private set; }

    public MomentComment Comment { get; private set; } = null!;

    private MomentCommentLike() { }

    public MomentCommentLike(Guid commentId, Guid userId)
    {
        if (commentId == Guid.Empty)
            throw new ArgumentException("Comment ID is required.", nameof(commentId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        CommentId = commentId;
        UserId = userId;
    }
}
