namespace Shandis.Application.Features.Memories;

public sealed class MyMemoriesResult
{
    public IReadOnlyList<MyMemoryItemResult> Items { get; init; }
        = Array.Empty<MyMemoryItemResult>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages =>
        PageSize == 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);
}