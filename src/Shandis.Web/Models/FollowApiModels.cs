namespace Shandis.Web.Models;

public sealed class FollowRelationshipItem
{
    public Guid UserId { get; set; }
    public bool IsFollowing { get; set; }
}

public sealed class FollowUsersResponse
{
    public Guid UserId { get; set; }
    public string Relationship { get; set; } = string.Empty;
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<FollowUserItem> Users { get; set; } = [];
}

public sealed class FollowUserItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public bool IsFollowing { get; set; }
}
