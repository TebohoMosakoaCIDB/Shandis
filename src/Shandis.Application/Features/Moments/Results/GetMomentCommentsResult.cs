namespace Shandis.Application.Features.Moments.Results;

public class GetMomentCommentsResult
{
    public Guid MomentId { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<MomentCommentResult> Comments { get; set; } = [];
}
