using Microsoft.EntityFrameworkCore;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Infrastructure.Persistence.Repositories;

public class MemoryRepository : IMemoryRepository
{
    private readonly ShandisDbContext _dbContext;

    public MemoryRepository(ShandisDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CategoryExistsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MemoryCategories.AnyAsync(
            x => x.Id == categoryId && x.IsActive,
            cancellationToken);
    }

    public Task<bool> PlaceExistsAsync(
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Places.AnyAsync(
            x => x.Id == placeId,
            cancellationToken);
    }

    public async Task AddAsync(
        Memory memory,
        MemoryParticipant ownerParticipant,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.Memories.AddAsync(memory, cancellationToken);

        await _dbContext.MemoryParticipants.AddAsync(
            ownerParticipant,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Memory?> GetByIdAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Memories
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Place)
            .FirstOrDefaultAsync(
                x => x.Id == memoryId && x.DeletedAtUtc == null,
                cancellationToken);
    }

    public Task<Memory?> GetTrackedByIdAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Memories
            .Include(x => x.Category)
            .Include(x => x.Place)
            .FirstOrDefaultAsync(
                x => x.Id == memoryId && x.DeletedAtUtc == null,
                cancellationToken);
    }

    public Task<bool> IsActiveParticipantAsync(
        Guid memoryId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.MemoryParticipants.AnyAsync(
            x => x.MemoryId == memoryId &&
                 x.UserId == userId &&
                 x.Status == ParticipantStatus.Active &&
                 x.DeletedAtUtc == null,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Memory> Items, int TotalCount)> GetMyMemoriesAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Memories
            .AsNoTracking()
            .Where(memory =>
                memory.DeletedAtUtc == null &&
                (
                    memory.OwnerId == userId ||
                    memory.Participants.Any(participant =>
                        participant.UserId == userId &&
                        participant.Role == ParticipantRole.Contributor &&
                        participant.Status == ParticipantStatus.Active &&
                        participant.DeletedAtUtc == null)
                ));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(x => x.Category)
            .Include(x => x.Place)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}