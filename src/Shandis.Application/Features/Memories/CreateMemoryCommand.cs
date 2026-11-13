
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Memories;

public sealed class CreateMemoryCommand
{
    public Guid OwnerId { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? PlaceId { get; init; }

    public MemoryVisibility Visibility { get; init; }
        = MemoryVisibility.Private;

    public MemoryContributionPolicy ContributionPolicy { get; init; }
        = MemoryContributionPolicy.InvitationOnly;

    public DateTime? StartDateUtc { get; init; }

    public DateTime? EndDateUtc { get; init; }

    public string TimeZoneId { get; init; }
        = "Africa/Johannesburg";
}
