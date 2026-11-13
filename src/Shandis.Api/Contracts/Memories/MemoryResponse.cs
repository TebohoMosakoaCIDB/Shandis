
using Shandis.Domain.Enums;

namespace Shandis.Api.Contracts.Memories;

public class MemoryResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid OwnerId { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? PlaceId { get; set; }

    public MemoryVisibility Visibility { get; set; }

    public MemoryContributionPolicy ContributionPolicy { get; set; }

    public MemoryStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
