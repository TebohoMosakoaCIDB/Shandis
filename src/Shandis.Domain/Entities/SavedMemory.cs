using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class SavedMemory : Entity
{
    public Guid UserId { get; private set; }
    public Guid MemoryId { get; private set; }

    public Memory Memory { get; private set; } = null!;

    private SavedMemory() { }

    public SavedMemory(Guid userId, Guid memoryId)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User ID is required.", nameof(userId));
        if (memoryId == Guid.Empty) throw new ArgumentException("Memory ID is required.", nameof(memoryId));
        UserId = userId;
        MemoryId = memoryId;
    }
}
