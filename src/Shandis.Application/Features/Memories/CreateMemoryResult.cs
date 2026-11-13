
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Memories;

public sealed class CreateMemoryResult
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }

    public Guid OwnerId { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? PlaceId { get; init; }

    public MemoryVisibility Visibility { get; init; }

    public MemoryContributionPolicy ContributionPolicy { get; init; }

    public MemoryStatus Status { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
