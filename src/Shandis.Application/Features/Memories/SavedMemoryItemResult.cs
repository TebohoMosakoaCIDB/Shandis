namespace Shandis.Application.Features.Memories;

public sealed class SavedMemoryItemResult
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CategoryName { get; set; }
    public string? PlaceName { get; set; }
    public int Visibility { get; set; }
    public DateTime? StartDateUtc { get; set; }
    public DateTime SavedAtUtc { get; set; }
    public string? CoverImageUrl { get; set; }
}

public sealed class SavedMemoriesResult
{
    public IReadOnlyList<SavedMemoryItemResult> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
