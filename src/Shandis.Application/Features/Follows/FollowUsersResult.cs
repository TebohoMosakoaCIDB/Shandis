namespace Shandis.Application.Features.Follows;

public sealed class FollowUserItem
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? ProfilePictureUrl { get; init; }
    public bool IsFollowing { get; init; }
}

public sealed class FollowUsersResult
{
    public Guid UserId { get; init; }
    public string Relationship { get; init; } = string.Empty;
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public IReadOnlyList<FollowUserItem> Users { get; init; } = [];
}
