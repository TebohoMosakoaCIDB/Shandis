using Microsoft.EntityFrameworkCore;
using Shandis.Domain.Entities;

namespace Shandis.Infrastructure.Persistence.Seeders;

public static class MemoryCategorySeeder
{
    public static async Task SeedAsync(
        ShandisDbContext context,
        CancellationToken cancellationToken = default)
    {
        var categories = new[]
        {
            new MemoryCategory("Parties", "Birthday celebrations and parties", 1),
            new MemoryCategory("Weddings", "Weddings and anniversaries", 2),
            new MemoryCategory("Family", "Family gatherings and experiences", 3),
            new MemoryCategory("Friends", "Memories shared with friends", 4),
            new MemoryCategory("Travel", "Trips, holidays and adventures", 5),
            new MemoryCategory("Concerts", "Concerts and live music events", 6),
            new MemoryCategory("Festivals", "Festivals and cultural celebrations", 7),
            new MemoryCategory("Sports", "Sports events and competitions", 8),
            new MemoryCategory("Graduations", "Graduations and academic achievements", 9),
            new MemoryCategory("Community", "Community events and gatherings", 10),
            new MemoryCategory("Other", "Other experiences and occasions", 11)
        };

        var existingNames = await context.MemoryCategories
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);

        var existingNamesSet = new HashSet<string>(
            existingNames,
            StringComparer.OrdinalIgnoreCase);

        var newCategories = categories
            .Where(category => !existingNamesSet.Contains(category.Name))
            .ToList();

        if (newCategories.Count == 0)
            return;

        await context.MemoryCategories.AddRangeAsync(
            newCategories,
            cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
    }
}
