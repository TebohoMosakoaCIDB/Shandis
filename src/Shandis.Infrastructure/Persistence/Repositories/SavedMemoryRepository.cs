using Microsoft.EntityFrameworkCore;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Infrastructure.Persistence.Repositories;

public sealed class SavedMemoryRepository(IDbContextFactory<ShandisDbContext> contextFactory) : ISavedMemoryRepository
{
    public async Task<bool> CanViewMemoryAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Memories.AnyAsync(m => m.Id == memoryId && m.DeletedAtUtc == null &&
            (m.OwnerId == userId ||
             (m.Visibility == MemoryVisibility.Public && (m.Status == MemoryStatus.Published || m.Status == MemoryStatus.Completed)) ||
             m.Participants.Any(p => p.UserId == userId && p.Status == ParticipantStatus.Active && p.DeletedAtUtc == null)),
            cancellationToken);
    }

    public async Task<bool> IsSavedAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.SavedMemories.AnyAsync(x => x.MemoryId == memoryId && x.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(SavedMemory savedMemory, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.SavedMemories.Add(savedMemory);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var saved = await db.SavedMemories.FirstOrDefaultAsync(x => x.MemoryId == memoryId && x.UserId == userId, cancellationToken);
        if (saved is null) return;
        db.SavedMemories.Remove(saved);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Memory> Items, int TotalCount)> GetSavedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.SavedMemories.AsNoTracking()
            .Where(s => s.UserId == userId && s.Memory.DeletedAtUtc == null &&
                (s.Memory.OwnerId == userId ||
                 (s.Memory.Visibility == MemoryVisibility.Public && (s.Memory.Status == MemoryStatus.Published || s.Memory.Status == MemoryStatus.Completed)) ||
                 s.Memory.Participants.Any(p => p.UserId == userId && p.Status == ParticipantStatus.Active && p.DeletedAtUtc == null)))
            .OrderByDescending(s => s.CreatedAtUtc);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(s => s.Memory)
            .Include(m => m.Category).Include(m => m.Place)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }
}
