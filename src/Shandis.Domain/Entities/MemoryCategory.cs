using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public class MemoryCategory : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int SortOrder { get; private set; }

    public ICollection<Memory> Memories { get; private set; } = new List<Memory>();

    private MemoryCategory() { }

    public MemoryCategory(string name, string? description = null, int sortOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        SortOrder = sortOrder;
    }

    public void Update(string name, string? description, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name is required.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        SortOrder = sortOrder;
        MarkAsUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkAsUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkAsUpdated();
    }
}
