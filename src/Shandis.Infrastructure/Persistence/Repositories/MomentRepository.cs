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


    public async Task AddCommentAsync(MomentComment comment, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.MomentComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MomentComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.MomentComments
            .AsNoTracking()
            .Include(x => x.Moment)
                .ThenInclude(x => x.Memory)
            .FirstOrDefaultAsync(x => x.Id == commentId && x.DeletedAtUtc == null, cancellationToken);
    }

    public async Task SaveCommentAsync(MomentComment comment, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.MomentComments.Attach(comment);
        var entry = dbContext.Entry(comment);
        entry.Property(x => x.Content).IsModified = true;
        entry.Property(x => x.UpdatedAtUtc).IsModified = true;
        entry.Property(x => x.DeletedAtUtc).IsModified = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<MomentCommentResult> Comments, int TotalCount)> GetCommentsAsync(
        Guid momentId, Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = dbContext.MomentComments.AsNoTracking()
            .Where(x => x.MomentId == momentId && x.ParentCommentId == null && x.DeletedAtUtc == null);

        var totalCount = await query.CountAsync(cancellationToken);
        var topLevel = await query.OrderBy(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var parentIds = topLevel.Select(x => x.Id).ToList();
        var replies = parentIds.Count == 0
            ? new List<MomentComment>()
            : await dbContext.MomentComments.AsNoTracking()
                .Where(x => x.MomentId == momentId &&
                            x.ParentCommentId != null &&
                            parentIds.Contains(x.ParentCommentId.Value) &&
                            x.DeletedAtUtc == null)
                .OrderBy(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken);

        var all = topLevel.Concat(replies).ToList();
        var authorIds = all.Select(x => x.UserId).Distinct().ToList();
        var users = await dbContext.Users.AsNoTracking()
            .Where(x => authorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var commentIds = all.Select(x => x.Id).ToList();
        var likeCounts = await dbContext.MomentCommentLikes.AsNoTracking()
            .Where(x => commentIds.Contains(x.CommentId))
            .GroupBy(x => x.CommentId)
            .Select(x => new { Id = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count, cancellationToken);

        var likedIds = await dbContext.MomentCommentLikes.AsNoTracking()
            .Where(x => commentIds.Contains(x.CommentId) && x.UserId == userId)
            .Select(x => x.CommentId)
            .ToListAsync(cancellationToken);

        MomentCommentResult Map(MomentComment comment)
        {
            users.TryGetValue(comment.UserId, out var author);
            return new MomentCommentResult
            {
                Id = comment.Id,
                MomentId = comment.MomentId,
                UserId = comment.UserId,
                ParentCommentId = comment.ParentCommentId,
                AuthorName = author?.DisplayName ??
                    (author is null ? "Unknown user" : $"{author.FirstName} {author.LastName}".Trim()),
                AuthorProfilePictureUrl = author?.ProfilePictureUrl,
                Content = comment.Content,
                CreatedAtUtc = comment.CreatedAtUtc,
                UpdatedAtUtc = comment.UpdatedAtUtc,
                LikeCount = likeCounts.GetValueOrDefault(comment.Id),
                IsLikedByCurrentUser = likedIds.Contains(comment.Id)
            };
        }

        var repliesByParent = replies.GroupBy(x => x.ParentCommentId!.Value)
            .ToDictionary(x => x.Key, x => x.ToList());

        var results = topLevel.Select(comment =>
        {
            var result = Map(comment);
            if (repliesByParent.TryGetValue(comment.Id, out var children))
                result.Replies = children.Select(Map).ToList();
            return result;
        }).ToList();

        return (results, totalCount);
    }

    public async Task<MomentCommentResult?> GetCommentResultAsync(
        Guid commentId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var comment = await dbContext.MomentComments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == commentId && x.DeletedAtUtc == null, cancellationToken);
        if (comment is null) return null;

        var author = await dbContext.Users.AsNoTracking()
            .Where(x => x.Id == comment.UserId)
            .Select(x => new { x.DisplayName, x.FirstName, x.LastName, x.ProfilePictureUrl })
            .FirstOrDefaultAsync(cancellationToken);

        var likes = dbContext.MomentCommentLikes.AsNoTracking().Where(x => x.CommentId == commentId);
        var result = new MomentCommentResult
        {
            Id = comment.Id,
            MomentId = comment.MomentId,
            UserId = comment.UserId,
            ParentCommentId = comment.ParentCommentId,
            AuthorName = author?.DisplayName ??
                (author is null ? "Unknown user" : $"{author.FirstName} {author.LastName}".Trim()),
            AuthorProfilePictureUrl = author?.ProfilePictureUrl,
            Content = comment.Content,
            CreatedAtUtc = comment.CreatedAtUtc,
            UpdatedAtUtc = comment.UpdatedAtUtc,
            LikeCount = await likes.CountAsync(cancellationToken),
            IsLikedByCurrentUser = await likes.AnyAsync(x => x.UserId == userId, cancellationToken)
        };

        if (comment.ParentCommentId is null)
        {
            var replies = await dbContext.MomentComments.AsNoTracking()
                .Where(x => x.ParentCommentId == commentId && x.DeletedAtUtc == null)
                .OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

            foreach (var reply in replies)
            {
                var replyAuthor = await dbContext.Users.AsNoTracking()
                    .Where(x => x.Id == reply.UserId)
                    .Select(x => new { x.DisplayName, x.FirstName, x.LastName, x.ProfilePictureUrl })
                    .FirstOrDefaultAsync(cancellationToken);
                var replyLikes = dbContext.MomentCommentLikes.AsNoTracking()
                    .Where(x => x.CommentId == reply.Id);
                result.Replies.Add(new MomentCommentResult
                {
                    Id = reply.Id,
                    MomentId = reply.MomentId,
                    UserId = reply.UserId,
                    ParentCommentId = reply.ParentCommentId,
                    AuthorName = replyAuthor?.DisplayName ??
                        (replyAuthor is null ? "Unknown user" : $"{replyAuthor.FirstName} {replyAuthor.LastName}".Trim()),
                    AuthorProfilePictureUrl = replyAuthor?.ProfilePictureUrl,
                    Content = reply.Content,
                    CreatedAtUtc = reply.CreatedAtUtc,
                    UpdatedAtUtc = reply.UpdatedAtUtc,
                    LikeCount = await replyLikes.CountAsync(cancellationToken),
                    IsLikedByCurrentUser = await replyLikes.AnyAsync(x => x.UserId == userId, cancellationToken)
                });
            }
        }

        return result;
    }

    public async Task SetCommentLikeAsync(
        Guid commentId, Guid userId, bool isLiked, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existing = await dbContext.MomentCommentLikes.FirstOrDefaultAsync(
            x => x.CommentId == commentId && x.UserId == userId, cancellationToken);

        if (isLiked)
        {
            if (existing is null) dbContext.MomentCommentLikes.Add(new MomentCommentLike(commentId, userId));
        }
        else if (existing is not null)
        {
            dbContext.MomentCommentLikes.Remove(existing);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MomentCommentLikeResult> GetCommentLikeSummaryAsync(
        Guid commentId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var likes = dbContext.MomentCommentLikes.AsNoTracking().Where(x => x.CommentId == commentId);
        return new MomentCommentLikeResult
        {
            CommentId = commentId,
            LikeCount = await likes.CountAsync(cancellationToken),
            IsLikedByCurrentUser = await likes.AnyAsync(x => x.UserId == userId, cancellationToken)
        };
    }

}
