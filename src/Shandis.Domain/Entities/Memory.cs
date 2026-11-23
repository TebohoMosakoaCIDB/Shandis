using Shandis.Domain.Common;
using Shandis.Domain.Enums;

namespace Shandis.Domain.Entities;

public class Memory : Entity
{
    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public Guid OwnerId { get; private set; }

    public Guid? CategoryId { get; private set; }

    public Guid? PlaceId { get; private set; }

    public MemoryVisibility Visibility { get; private set; }

    public MemoryContributionPolicy ContributionPolicy { get; private set; }

    public MemoryStatus Status { get; private set; }

    public DateTime? StartDateUtc { get; private set; }

    public DateTime? EndDateUtc { get; private set; }

    public string TimeZoneId { get; private set; } = "UTC";

    public Guid? CoverMediaId { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    // Navigation properties
    public MemoryCategory? Category { get; private set; }

    public Place? Place { get; private set; }

    public MemoryMedia? CoverMedia { get; private set; }

    public ICollection<MemoryMoment> Moments { get; private set; }
        = new List<MemoryMoment>();

    public ICollection<MemoryParticipant> Participants { get; private set; }
        = new List<MemoryParticipant>();

    public ICollection<MemoryInvitation> Invitations { get; private set; }
        = new List<MemoryInvitation>();

    public ICollection<MemoryComment> Comments { get; private set; }
        = new List<MemoryComment>();

    private Memory()
    {
    }

    public Memory(
        string title,
        Guid ownerId,
        MemoryVisibility visibility,
        MemoryContributionPolicy contributionPolicy,
        string timeZoneId = "UTC",
        string? description = null,
        Guid? categoryId = null,
        Guid? placeId = null,
        DateTime? startDateUtc = null,
        DateTime? endDateUtc = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Memory title is required.", nameof(title));

        if (ownerId == Guid.Empty)
            throw new ArgumentException("Memory owner is required.", nameof(ownerId));

        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Time zone is required.", nameof(timeZoneId));

        if (endDateUtc.HasValue &&
            startDateUtc.HasValue &&
            endDateUtc < startDateUtc)
        {
            throw new ArgumentException(
                "Memory end date cannot be earlier than its start date.");
        }

        Title = title.Trim();
        Description = description?.Trim();
        OwnerId = ownerId;
        Visibility = visibility;
        ContributionPolicy = contributionPolicy;
        TimeZoneId = timeZoneId;
        CategoryId = categoryId;
        PlaceId = placeId;
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;
        Status = MemoryStatus.Draft;
    }

    public void UpdateDetails(
        string title,
        string? description,
        Guid? categoryId,
        Guid? placeId,
        DateTime? startDateUtc,
        DateTime? endDateUtc)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Memory title is required.", nameof(title));

        if (endDateUtc.HasValue &&
            startDateUtc.HasValue &&
            endDateUtc < startDateUtc)
        {
            throw new ArgumentException(
                "Memory end date cannot be earlier than its start date.");
        }

        Title = title.Trim();
        Description = description?.Trim();
        CategoryId = categoryId;
        PlaceId = placeId;
        StartDateUtc = startDateUtc;
        EndDateUtc = endDateUtc;

        MarkAsUpdated();
    }

    public void UpdateVisibility(
        MemoryVisibility visibility,
        MemoryContributionPolicy contributionPolicy)
    {
        Visibility = visibility;
        ContributionPolicy = contributionPolicy;

        MarkAsUpdated();
    }

    public void SetCoverMedia(Guid? mediaId)
    {
        CoverMediaId = mediaId;
        MarkAsUpdated();
    }

    public void Publish()
    {
        if (Status != MemoryStatus.Draft)
            throw new InvalidOperationException(
                "Only draft Memories can be published.");

        Status = MemoryStatus.Published;
        PublishedAtUtc = DateTime.UtcNow;

        MarkAsUpdated();
    }

    public void Complete()
    {
        if (Status != MemoryStatus.Published)
            throw new InvalidOperationException(
                "Only published Memories can be completed.");

        Status = MemoryStatus.Completed;

        MarkAsUpdated();
    }

    public void Archive()
    {
        Status = MemoryStatus.Archived;
        MarkAsUpdated();
    }

    public void UpdateTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            throw new ArgumentException("Time zone is required.", nameof(timeZoneId));

        TimeZoneId = timeZoneId.Trim();

        MarkAsUpdated();
    }
}
