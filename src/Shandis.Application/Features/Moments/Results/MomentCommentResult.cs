namespace Shandis.Application.Features.Moments.Results;

public class MomentCommentResult
{
    public Guid Id { get; set; }
    public Guid MomentId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorProfilePictureUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public List<MomentCommentResult> Replies { get; set; } = [];
}
