using Shandis.Domain.Common;
using Shandis.Domain.Enums;

namespace Shandis.Domain.Entities;

public class MemoryInvitation : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public Guid? InvitedUserId { get; private set; }
    public string InvitedEmail { get; private set; } = string.Empty;
    public InvitationRole AssignedRole { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public InvitationStatus Status { get; private set; }
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? AcceptedAtUtc { get; private set; }

    public Memory Memory { get; private set; } = null!;

    private MemoryInvitation() { }

    public MemoryInvitation(
        Guid memoryId,
        Guid invitedByUserId,
        Guid? invitedUserId,
        string invitedEmail,
        InvitationRole assignedRole,
        string tokenHash,
        DateTime expiresAtUtc)
    {
        if (memoryId == Guid.Empty)
            throw new ArgumentException("Memory ID is required.", nameof(memoryId));

        if (invitedByUserId == Guid.Empty)
            throw new ArgumentException("Inviter user ID is required.", nameof(invitedByUserId));

        if (invitedUserId == Guid.Empty)
            throw new ArgumentException("Invited user ID cannot be an empty GUID.", nameof(invitedUserId));

        if (string.IsNullOrWhiteSpace(invitedEmail))
            throw new ArgumentException("An invitation email address is required.", nameof(invitedEmail));

        if (string.IsNullOrWhiteSpace(tokenHash))
            throw new ArgumentException("Invitation token hash is required.", nameof(tokenHash));

        if (expiresAtUtc <= DateTime.UtcNow)
            throw new ArgumentException("Invitation expiry must be in the future.", nameof(expiresAtUtc));

        MemoryId = memoryId;
        InvitedByUserId = invitedByUserId;
        InvitedUserId = invitedUserId;
        InvitedEmail = invitedEmail.Trim().ToLowerInvariant();
        AssignedRole = assignedRole;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        Status = InvitationStatus.Pending;
    }

    public void Accept(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        EnsurePending();

        if (ExpiresAtUtc <= DateTime.UtcNow)
        {
            Expire();
            throw new InvalidOperationException("This invitation has expired.");
        }

        if (InvitedUserId.HasValue && InvitedUserId.Value != userId)
            throw new InvalidOperationException("This invitation belongs to another user.");

        InvitedUserId = userId;
        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Decline()
    {
        EnsurePending();
        Status = InvitationStatus.Declined;
        MarkAsUpdated();
    }

    public void Revoke()
    {
        EnsurePending();
        Status = InvitationStatus.Revoked;
        MarkAsUpdated();
    }

    public void Expire()
    {
        EnsurePending();

        if (ExpiresAtUtc > DateTime.UtcNow)
            throw new InvalidOperationException(
                "An invitation cannot expire before its expiry date.");

        Status = InvitationStatus.Expired;
        MarkAsUpdated();
    }

    private void EnsurePending()
    {
        if (Status != InvitationStatus.Pending)
            throw new InvalidOperationException(
                "Only pending invitations can be changed.");
    }
}
