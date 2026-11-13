
namespace Shandis.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; protected set; }

    public DateTime? DeletedAtUtc { get; protected set; }

    public void MarkAsUpdated()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsDeleted()
    {
        DeletedAtUtc = DateTime.UtcNow;
    }
}
