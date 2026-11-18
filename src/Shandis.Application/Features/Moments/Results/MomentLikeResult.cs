namespace Shandis.Application.Features.Moments.Results;

public class MomentLikeResult
{
    public Guid MomentId { get; set; }

    public int LikeCount { get; set; }

    public bool IsLikedByCurrentUser { get; set; }
}
