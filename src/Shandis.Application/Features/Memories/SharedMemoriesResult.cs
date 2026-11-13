namespace Shandis.Application.Features.Memories;

public sealed class SharedMemoriesResult
{
    public IReadOnlyList<SharedMemoryItemResult> Items { get; init; }
        = Array.Empty<SharedMemoryItemResult>();

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }

    public int TotalPages =>
        PageSize == 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);
}
