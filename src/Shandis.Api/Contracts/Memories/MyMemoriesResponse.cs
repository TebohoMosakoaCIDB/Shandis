namespace Shandis.Api.Contracts.Memories;

public class MyMemoriesResponse
{
    public IReadOnlyList<MyMemoryItemResponse> Items { get; set; }
        = Array.Empty<MyMemoryItemResponse>();

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}