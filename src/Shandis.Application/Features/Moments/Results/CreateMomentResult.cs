
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Moments.Results;

public class CreateMomentResult
{
    public Guid Id { get; set; }

    public Guid MemoryId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public MomentType MomentType { get; set; }

    public MomentSourceType SourceType { get; set; }

    public MomentStatus Status { get; set; }

    public DateTime? HappenedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    public int LikeCount { get; set; }

    public int CommentCount { get; set; }

    public bool IsLikedByCurrentUser { get; set; }

    public List<MomentMediaResult> Media { get; set; } = [];
}

public class MomentMediaResult
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public MediaType MediaType { get; set; }

    public int SortOrder { get; set; }

    public string Url { get; set; } = string.Empty;
}
