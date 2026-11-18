using Microsoft.EntityFrameworkCore;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Application.Features.Moments.Results;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;
using Shandis.Infrastructure.Persistence;

namespace Shandis.Infrastructure.Persistence.Repositories;

public class MomentRepository : IMomentRepository
{
    private readonly IDbContextFactory<ShandisDbContext> _contextFactory;

    public MomentRepository(
        IDbContextFactory<ShandisDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<MomentContributionContext?> GetContributionContextAsync(
        Guid memoryId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var memory = await dbContext.Memories
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == memoryId &&
                     x.DeletedAtUtc == null,
                cancellationToken);

        if (memory is null)
            return null;

        var participant = await dbContext.MemoryParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.MemoryId == memoryId &&
                     x.UserId == userId,
                cancellationToken);

        return new MomentContributionContext
        {
            Memory = memory,
            Participant = participant
        };
    }

    public async Task AddAsync(
        MemoryMoment moment,
        MemoryParticipant? participant,
        bool isNewParticipant,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            dbContext.MemoryMoments.Add(moment);

            if (participant is not null)
            {
                if (isNewParticipant)
                {
                    dbContext.MemoryParticipants.Add(participant);
                }
                else
                {
                    dbContext.MemoryParticipants.Attach(participant);

                    dbContext.Entry(participant).State =
                        EntityState.Modified;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<(IReadOnlyList<MemoryMoment> Moments, int TotalCount)> GetByMemoryAsync(
    Guid memoryId,
    Guid userId,
    Guid memoryOwnerId,
    int page,
    int pageSize,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.MemoryMoments
            .AsNoTracking()
            .Include(x => x.Media)
            .Where(x =>
                x.MemoryId == memoryId &&
                x.DeletedAtUtc == null &&
                x.Status != MomentStatus.Removed);

        // Published Moments are visible to users who can view the Memory.
        // Pending and rejected Moments are only visible to their author
        // and the Memory owner.
        query = query.Where(x =>
            x.Status == MomentStatus.Published ||
            ((userId == memoryOwnerId || x.CreatedByUserId == userId) &&
             (x.Status == MomentStatus.Pending ||
              x.Status == MomentStatus.Rejected)));

        var totalCount = await query.CountAsync(cancellationToken);

        var moments = await query
            .OrderByDescending(x => x.HappenedAtUtc ?? x.CreatedAtUtc)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (moments, totalCount);
    }

    public async Task<MemoryMoment?> GetByIdAsync(
        Guid momentId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.MemoryMoments
            .AsNoTracking()
            .Include(x => x.Media)
            .Include(x => x.Memory)
            .FirstOrDefaultAsync(
                x => x.Id == momentId &&
                     x.DeletedAtUtc == null,
                cancellationToken);
    }

    public async Task SaveModerationAsync(
    MemoryMoment moment,
    MemoryParticipant? participant,
    bool isNewParticipant,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            dbContext.MemoryMoments.Attach(moment);

            dbContext.Entry(moment)
                .Property(x => x.Status)
                .IsModified = true;

            dbContext.Entry(moment)
                .Property(x => x.PublishedAtUtc)
                .IsModified = true;

            dbContext.Entry(moment)
                .Property(x => x.UpdatedAtUtc)
                .IsModified = true;

            if (participant is not null)
            {
                if (isNewParticipant)
                {
                    dbContext.MemoryParticipants.Add(participant);
                }
                else
                {
                    dbContext.MemoryParticipants.Attach(participant);

                    var entry = dbContext.Entry(participant);

                    entry.Property(x => x.Role).IsModified = true;
                    entry.Property(x => x.Status).IsModified = true;
                    entry.Property(x => x.JoinSource).IsModified = true;
                    entry.Property(x => x.JoinedAtUtc).IsModified = true;
                    entry.Property(x => x.UpdatedAtUtc).IsModified = true;
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task SetLikeAsync(
        Guid momentId,
        Guid userId,
        bool isLiked,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var existingReaction = await dbContext.MomentReactions
            .FirstOrDefaultAsync(
                x => x.MomentId == momentId && x.UserId == userId,
                cancellationToken);

        if (isLiked)
        {
            if (existingReaction is null)
            {
                dbContext.MomentReactions.Add(
                    new MomentReaction(momentId, userId, ReactionType.Like));
            }
        }
        else if (existingReaction is not null)
        {
            dbContext.MomentReactions.Remove(existingReaction);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MomentLikeResult> GetLikeSummaryAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var reactions = dbContext.MomentReactions
            .AsNoTracking()
            .Where(x =>
                x.MomentId == momentId &&
                x.ReactionType == ReactionType.Like);

        var likeCount = await reactions.CountAsync(cancellationToken);

        var isLikedByCurrentUser = await reactions.AnyAsync(
            x => x.UserId == userId,
            cancellationToken);

        return new MomentLikeResult
        {
            MomentId = momentId,
            LikeCount = likeCount,
            IsLikedByCurrentUser = isLikedByCurrentUser
        };
    }

}
