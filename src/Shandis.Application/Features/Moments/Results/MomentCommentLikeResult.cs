namespace Shandis.Application.Features.Moments.Results;

public class MomentCommentLikeResult
{
    public Guid CommentId { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}
