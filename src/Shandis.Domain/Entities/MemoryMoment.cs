
using Shandis.Domain.Common;
using Shandis.Domain.Enums;

namespace Shandis.Domain.Entities;

public class MemoryMoment : Entity
{
    public Guid MemoryId { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public string? Title { get; private set; }

    public string? Content { get; private set; }

    public MomentType MomentType { get; private set; }

    public MomentSourceType SourceType { get; private set; }

    public MomentStatus Status { get; private set; }

    public DateTime? HappenedAtUtc { get; private set; }

    public DateTime? PublishedAtUtc { get; private set; }

    public Memory Memory { get; private set; } = null!;

    public ICollection<MomentMedia> Media { get; private set; }
        = new List<MomentMedia>();

    public ICollection<MomentReaction> Reactions { get; private set; }
        = new List<MomentReaction>();

    private MemoryMoment()
    {
    }

    public MemoryMoment(
        Guid memoryId,
        Guid createdByUserId,
        MomentType momentType,
        MomentSourceType sourceType,
        string? title = null,
        string? content = null,
        DateTime? happenedAtUtc = null,
        bool requiresApproval = false)
    {
        if (memoryId == Guid.Empty)
            throw new ArgumentException("Memory is required.");

        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("Moment author is required.");

        MemoryId = memoryId;
        CreatedByUserId = createdByUserId;
        MomentType = momentType;
        SourceType = sourceType;
        Title = title?.Trim();
        Content = content?.Trim();
        HappenedAtUtc = happenedAtUtc;

        Status = requiresApproval
            ? MomentStatus.Pending
            : MomentStatus.Published;

        if (Status == MomentStatus.Published)
            PublishedAtUtc = DateTime.UtcNow;
    }

    public void UpdateContent(string? title, string? content)
    {
        if (Status == MomentStatus.Removed)
            throw new InvalidOperationException(
                "Removed Moments cannot be edited.");

        Title = title?.Trim();
        Content = content?.Trim();

        MarkAsUpdated();
    }

    public void Approve()
    {
        if (Status != MomentStatus.Pending)
            throw new InvalidOperationException(
                "Only pending Moments can be approved.");

        Status = MomentStatus.Published;
        PublishedAtUtc = DateTime.UtcNow;

        MarkAsUpdated();
    }

    public void Reject()
    {
        if (Status != MomentStatus.Pending)
            throw new InvalidOperationException(
                "Only pending Moments can be rejected.");

        Status = MomentStatus.Rejected;

        MarkAsUpdated();
    }

    public void Remove()
    {
        Status = MomentStatus.Removed;
        MarkAsDeleted();
    }
}