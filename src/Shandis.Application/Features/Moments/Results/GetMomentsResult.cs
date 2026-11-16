namespace Shandis.Application.Features.Moments.Results;

public class GetMomentsResult
{
    public Guid MemoryId { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages =>
        (int)Math.Ceiling(TotalCount / (double)PageSize);

    public List<CreateMomentResult> Moments { get; set; } = [];
}