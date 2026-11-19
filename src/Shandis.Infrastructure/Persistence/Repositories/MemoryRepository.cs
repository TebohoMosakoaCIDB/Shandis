
using Microsoft.EntityFrameworkCore;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;
using Shandis.Infrastructure.Persistence;

namespace Shandis.Infrastructure.Persistence.Repositories;

public class MemoryRepository : IMemoryRepository
{
    private readonly IDbContextFactory<ShandisDbContext> _contextFactory;

    public MemoryRepository(
        IDbContextFactory<ShandisDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> CategoryExistsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.MemoryCategories.AnyAsync(
            x => x.Id == categoryId && x.IsActive,
            cancellationToken);
    }

    public async Task<bool> PlaceExistsAsync(
        Guid placeId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Places.AnyAsync(
            x => x.Id == placeId,
            cancellationToken);
    }

    public async Task AddAsync(
        Memory memory,
        MemoryParticipant ownerParticipant,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.Memories.AddAsync(
            memory,
            cancellationToken);

        await dbContext.MemoryParticipants.AddAsync(
            ownerParticipant,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Memory?> GetByIdAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Memories
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Place)
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(
                x => x.Id == memoryId && x.DeletedAtUtc == null,
                cancellationToken);
    }

    public async Task<bool> IsActiveParticipantAsync(
        Guid memoryId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.MemoryParticipants.AnyAsync(
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
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.Memories
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
            .Include(x => x.Participants)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task UpdateAsync(
        Memory memory,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.Memories.Attach(memory);

        dbContext.Entry(memory).State = EntityState.Modified;

        await dbContext.SaveChangesAsync(cancellationToken);
    }


    public async Task<(IReadOnlyList<Memory> Items, int TotalCount)> GetSharedWithMeAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.Memories
            .AsNoTracking()
            .Where(memory =>
                memory.DeletedAtUtc == null &&
                memory.Visibility == MemoryVisibility.Private &&
                memory.Participants.Any(participant =>
                    participant.UserId == userId &&
                    participant.Role != ParticipantRole.Owner &&
                    participant.JoinSource == ParticipantJoinSource.Invitation &&
                    participant.Status == ParticipantStatus.Active &&
                    participant.DeletedAtUtc == null));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(x => x.Category)
            .Include(x => x.Place)
            .Include(x => x.Participants)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

}
