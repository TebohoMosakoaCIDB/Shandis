namespace Shandis.Api.Contracts.Memories;

public class SharedMemoriesResponse
{
    public List<SharedMemoryItemResponse> Items { get; set; } = [];

    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
