
using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class DirectMessage : Entity
{
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTime SentAtUtc { get; private set; }
    public DateTime? EditedAtUtc { get; private set; }

    public Conversation Conversation { get; private set; } = null!;

    private DirectMessage() { }

    public DirectMessage(
        Guid conversationId,
        Guid senderId,
        string content)
    {
        if (conversationId == Guid.Empty)
            throw new ArgumentException(
                "Conversation ID is required.", nameof(conversationId));

        if (senderId == Guid.Empty)
            throw new ArgumentException(
                "Sender ID is required.", nameof(senderId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException(
                "Message content is required.", nameof(content));

        ConversationId = conversationId;
        SenderId = senderId;
        Content = content.Trim();
        SentAtUtc = DateTime.UtcNow;
    }

    public void Edit(string content)
    {
        if (DeletedAtUtc.HasValue)
            throw new InvalidOperationException(
                "A deleted message cannot be edited.");

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException(
                "Message content is required.", nameof(content));

        Content = content.Trim();
        EditedAtUtc = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void Delete()
    {
        if (DeletedAtUtc.HasValue)
            return;

        MarkAsDeleted();
    }
}
