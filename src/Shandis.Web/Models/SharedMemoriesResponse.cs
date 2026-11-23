namespace Shandis.Web.Models;

public sealed class SharedMemoriesResponse
{
    public List<SharedMemoryItem> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public sealed class SharedMemoryItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public string? CategoryName { get; set; }
    public string? PlaceName { get; set; }
    public int Visibility { get; set; }
    public int ContributionPolicy { get; set; }
    public int Status { get; set; }
    public int MyRole { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public Guid? CoverMediaId { get; set; }
    public string? CoverImageUrl { get; set; }
}
