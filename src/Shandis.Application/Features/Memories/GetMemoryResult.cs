
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Memories;

public sealed class GetMemoryResult
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid OwnerId { get; init; }

    public ParticipantRole? MyRole { get; init; }

    public Guid? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public Guid? PlaceId { get; init; }

    public string? PlaceName { get; init; }

    public string? PlaceAddress { get; init; }

    public MemoryVisibility Visibility { get; init; }

    public MemoryContributionPolicy ContributionPolicy { get; init; }

    public MemoryStatus Status { get; init; }

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public string TimeZoneId { get; init; } = string.Empty;

    public Guid? CoverMediaId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? PublishedAtUtc { get; init; }
}
