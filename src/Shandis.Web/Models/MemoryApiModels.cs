using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Shandis.Web.Models;

public sealed class MyMemoriesResponse
{
    public List<MyMemoryItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public sealed class MyMemoryItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public bool IsOwner { get; set; }
    public string? CategoryName { get; set; }
    public string? PlaceName { get; set; }
    public int Visibility { get; set; }
    public int ContributionPolicy { get; set; }
    public int Status { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public Guid? CoverMediaId { get; set; }
    public string? CoverImageUrl { get; set; }
}

public sealed class MemoryDetails
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public int? MyRole { get; set; }
    public string? CategoryName { get; set; }
    public string? PlaceName { get; set; }
    public string? PlaceAddress { get; set; }
    public int Visibility { get; set; }
    public int ContributionPolicy { get; set; }
    public int Status { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public string TimeZoneId { get; set; } = string.Empty;
    public Guid? CoverMediaId { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
}

public sealed class GetMomentsResponse
{
    public Guid MemoryId { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<MomentItem> Moments { get; set; } = [];
}

public sealed class MomentItem
{
    public Guid Id { get; set; }
    public Guid MemoryId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public int MomentType { get; set; }
    public int SourceType { get; set; }
    public int Status { get; set; }
    public DateTime? HappenedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
    public List<MomentMediaItem> Media { get; set; } = [];
}

public sealed class MomentMediaItem
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int MediaType { get; set; }
    public int SortOrder { get; set; }
    public string Url { get; set; } = string.Empty;
}


public sealed class MomentLikeResult
{
    public Guid MomentId { get; set; }
    public int LikeCount { get; set; }
    public bool IsLikedByCurrentUser { get; set; }
}

public sealed class MomentCommentsResponse
{
    public Guid MomentId { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<MomentCommentItem> Comments { get; set; } = [];
}

public sealed class MomentCommentItem
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
    public List<MomentCommentItem> Replies { get; set; } = [];
}

public sealed class CreateMomentCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}


public sealed class CreateMemoryRequest
{
    [Required(ErrorMessage = "Give your Memory a name.")]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
    public string Visibility { get; set; } = "Private";
    public string ContributionPolicy { get; set; } = "InvitationOnly";
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public string TimeZoneId { get; set; } = "Africa/Johannesburg";
}

public sealed class CreatedMemoryResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public int Visibility { get; set; }
    public int ContributionPolicy { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
