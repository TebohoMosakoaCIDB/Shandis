namespace Shandis.Application.Features.Memories;

public sealed class UpdateMemoryCommand
{
    public Guid MemoryId { get; init; }

    public Guid RequestingUserId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? PlaceId { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public string TimeZoneId { get; init; } = "Africa/Johannesburg";
}