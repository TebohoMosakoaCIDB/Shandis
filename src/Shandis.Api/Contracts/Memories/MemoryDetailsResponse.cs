
using Shandis.Domain.Enums;

namespace Shandis.Api.Contracts.Memories;

public class MemoryDetailsResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public ParticipantRole? MyRole { get; set; }

    public Guid? CategoryId { get; set; }

    public string? CategoryName { get; set; }

    public Guid? PlaceId { get; set; }

    public string? PlaceName { get; set; }

    public string? PlaceAddress { get; set; }

    public MemoryVisibility Visibility { get; set; }

    public MemoryContributionPolicy ContributionPolicy { get; set; }

    public MemoryStatus Status { get; set; }

    public DateTime? StartDateUtc { get; set; }

    public DateTime? EndDateUtc { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public Guid? CoverMediaId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }
}
