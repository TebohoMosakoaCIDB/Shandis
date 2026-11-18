using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class MomentComment : Entity
{
    public const int MaxContentLength = 2000;

    public Guid MomentId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ParentCommentId { get; private set; }
    public string Content { get; private set; } = string.Empty;

    public MemoryMoment Moment { get; private set; } = null!;
    public MomentComment? ParentComment { get; private set; }
    public ICollection<MomentComment> Replies { get; private set; } = new List<MomentComment>();
    public ICollection<MomentCommentLike> Likes { get; private set; } = new List<MomentCommentLike>();

    private MomentComment() { }

    public MomentComment(Guid momentId, Guid userId, string content, Guid? parentCommentId = null)
    {
        if (momentId == Guid.Empty)
            throw new ArgumentException("Moment ID is required.", nameof(momentId));
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));
        ValidateContent(content);

        if (parentCommentId == Guid.Empty)
            throw new ArgumentException("Parent comment ID must be a valid ID.", nameof(parentCommentId));

        MomentId = momentId;
        UserId = userId;
        Content = content.Trim();
        ParentCommentId = parentCommentId;
    }

    public void UpdateContent(string content)
    {
        if (DeletedAtUtc.HasValue)
            throw new InvalidOperationException("A deleted comment cannot be edited.");

        ValidateContent(content);
        Content = content.Trim();
        MarkAsUpdated();
    }

    public void Delete()
    {
        if (DeletedAtUtc.HasValue)
            return;

        MarkAsDeleted();
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content is required.", nameof(content));

        if (content.Trim().Length > MaxContentLength)
            throw new ArgumentException($"Comment content cannot exceed {MaxContentLength} characters.", nameof(content));
    }
}
