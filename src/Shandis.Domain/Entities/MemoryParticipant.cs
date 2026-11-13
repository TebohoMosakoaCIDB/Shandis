using Shandis.Domain.Common;
using Shandis.Domain.Enums;

namespace Shandis.Domain.Entities;

public class MemoryParticipant : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid UserId { get; private set; }
    public ParticipantRole Role { get; private set; }
    public ParticipantStatus Status { get; private set; }
    public ParticipantJoinSource JoinSource { get; private set; }
    public DateTime? JoinedAtUtc { get; private set; }

    public Memory Memory { get; private set; } = null!;

    private MemoryParticipant() { }

    public MemoryParticipant(
        Guid memoryId,
        Guid userId,
        ParticipantRole role,
        ParticipantJoinSource joinSource)
    {
        if (memoryId == Guid.Empty)
            throw new ArgumentException("Memory ID is required.", nameof(memoryId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (role == ParticipantRole.Owner && joinSource != ParticipantJoinSource.Owner)
            throw new ArgumentException("An owner must have the Owner join source.");

        if (role != ParticipantRole.Owner && joinSource == ParticipantJoinSource.Owner)
            throw new ArgumentException("Only an owner can have the Owner join source.");

        MemoryId = memoryId;
        UserId = userId;
        Role = role;
        JoinSource = joinSource;
        Status = ParticipantStatus.Active;
        JoinedAtUtc = DateTime.UtcNow;
    }

    public void Remove()
    {
        if (Role == ParticipantRole.Owner)
            throw new InvalidOperationException("The Memory owner cannot be removed.");

        Status = ParticipantStatus.Removed;
        MarkAsUpdated();
    }

    public void Reactivate(ParticipantRole role, ParticipantJoinSource joinSource)
    {
        if (role == ParticipantRole.Owner)
            throw new InvalidOperationException("An existing participant cannot be reactivated as the owner.");

        if (joinSource == ParticipantJoinSource.Owner)
            throw new ArgumentException("The Owner join source is reserved for the Memory owner.");

        Role = role;
        JoinSource = joinSource;
        Status = ParticipantStatus.Active;
        JoinedAtUtc = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void ChangeRole(ParticipantRole role)
    {
        if (Role == ParticipantRole.Owner)
            throw new InvalidOperationException("The Memory owner's role cannot be changed.");

        if (role == ParticipantRole.Owner)
            throw new InvalidOperationException("Ownership transfer must be handled separately.");

        Role = role;
        MarkAsUpdated();
    }
}
