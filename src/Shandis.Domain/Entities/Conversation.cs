
using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class Conversation : Entity
{
    public Guid UserOneId { get; private set; }
    public Guid UserTwoId { get; private set; }

    public DateTime? LastMessageAtUtc { get; private set; }

    public bool IsActive { get; private set; } = true;

    public ICollection<DirectMessage> Messages { get; private set; }
        = new List<DirectMessage>();

    private Conversation() { }

    public Conversation(Guid firstUserId, Guid secondUserId)
    {
        if (firstUserId == Guid.Empty)
            throw new ArgumentException("First user ID is required.", nameof(firstUserId));

        if (secondUserId == Guid.Empty)
            throw new ArgumentException("Second user ID is required.", nameof(secondUserId));

        if (firstUserId == secondUserId)
            throw new InvalidOperationException(
                "A conversation must have two different users.");

        // Store the pair in a consistent order.
        if (firstUserId.CompareTo(secondUserId) < 0)
        {
            UserOneId = firstUserId;
            UserTwoId = secondUserId;
        }
        else
        {
            UserOneId = secondUserId;
            UserTwoId = firstUserId;
        }
    }

    public bool HasParticipant(Guid userId)
    {
        return UserOneId == userId || UserTwoId == userId;
    }

    public Guid GetOtherParticipantId(Guid userId)
    {
        if (UserOneId == userId)
            return UserTwoId;

        if (UserTwoId == userId)
            return UserOneId;

        throw new InvalidOperationException(
            "The user is not a participant in this conversation.");
    }

    public void RecordMessageSent(DateTime sentAtUtc)
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "Messages cannot be sent to an inactive conversation.");

        LastMessageAtUtc = sentAtUtc;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }

    public void Reactivate()
    {
        IsActive = true;
        MarkAsUpdated();
    }
}
