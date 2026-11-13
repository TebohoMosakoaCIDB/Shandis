
using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class MemoryComment : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;

    public Memory Memory { get; private set; } = null!;

    private MemoryComment() { }

    public MemoryComment(Guid memoryId, Guid userId, string content)
    {
        if (memoryId == Guid.Empty)
            throw new ArgumentException("Memory ID is required.", nameof(memoryId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User ID is required.", nameof(userId));

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content is required.", nameof(content));

        MemoryId = memoryId;
        UserId = userId;
        Content = content.Trim();
    }

    public void UpdateContent(string content)
    {
        if (DeletedAtUtc.HasValue)
            throw new InvalidOperationException("A deleted comment cannot be edited.");

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content is required.", nameof(content));

        Content = content.Trim();
        MarkAsUpdated();
    }

    public void Delete()
    {
        if (DeletedAtUtc.HasValue)
            return;

        MarkAsDeleted();
    }
}
