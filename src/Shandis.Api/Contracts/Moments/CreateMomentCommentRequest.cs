namespace Shandis.Api.Contracts.Moments;

public class CreateMomentCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}
