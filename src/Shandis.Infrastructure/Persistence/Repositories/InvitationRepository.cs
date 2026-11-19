
using Microsoft.EntityFrameworkCore;
using Shandis.Application.Abstractions.Persistence;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Infrastructure.Persistence.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly IDbContextFactory<ShandisDbContext> _contextFactory;

    public InvitationRepository(
        IDbContextFactory<ShandisDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Guid?> GetMemoryOwnerIdAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Memories
            .Where(x => x.Id == memoryId && x.DeletedAtUtc == null)
            .Select(x => (Guid?)x.OwnerId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> FindUserIdByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedEmail = email.Trim().ToUpperInvariant();

        return await dbContext.Users
            .Where(x => x.NormalizedEmail == normalizedEmail)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Guid?> FindUserIdByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedUserName = userName.Trim().TrimStart('@').ToUpperInvariant();

        return await dbContext.Users
            .Where(x => x.NormalizedUserName == normalizedUserName)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasPendingInvitationAsync(
        Guid memoryId,
        string email,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await dbContext.MemoryInvitations.AnyAsync(
            x => x.MemoryId == memoryId &&
                 x.InvitedEmail == normalizedEmail &&
                 x.Status == InvitationStatus.Pending &&
                 x.ExpiresAtUtc > DateTime.UtcNow &&
                 x.DeletedAtUtc == null,
            cancellationToken);
    }

    public async Task AddAsync(
        MemoryInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        await dbContext.MemoryInvitations.AddAsync(
            invitation,
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryInvitation>> GetForMemoryAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.MemoryInvitations
            .AsNoTracking()
            .Include(x => x.Memory)
            .Where(x =>
                x.MemoryId == memoryId &&
                x.DeletedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MemoryInvitation>> GetReceivedAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return await dbContext.MemoryInvitations
            .AsNoTracking()
            .Include(x => x.Memory)
            .Where(x =>
                x.DeletedAtUtc == null &&
                x.Status == InvitationStatus.Pending &&
                (
                    x.InvitedUserId == userId ||
                    x.InvitedEmail == normalizedEmail
                ))
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetUserEmailAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Users
            .Where(x => x.Id == userId)
            .Select(x => x.Email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<string?> GetMemoryTitleAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.Memories
            .Where(x => x.Id == memoryId && x.DeletedAtUtc == null)
            .Select(x => x.Title)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<MemoryInvitation?> GetByIdAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.MemoryInvitations
            .AsNoTracking()
            .Include(x => x.Memory)
            .FirstOrDefaultAsync(
                x => x.Id == invitationId &&
                     x.DeletedAtUtc == null,
                cancellationToken);
    }

    public async Task<MemoryParticipant?> GetParticipantAsync(
        Guid memoryId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.MemoryParticipants
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.MemoryId == memoryId &&
                     x.UserId == userId,
                cancellationToken);
    }

    public async Task AcceptAsync(
        MemoryInvitation invitation,
        MemoryParticipant participant,
        bool isExistingParticipant,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        dbContext.MemoryInvitations.Attach(invitation);
        dbContext.Entry(invitation).State = EntityState.Modified;

        if (isExistingParticipant)
        {
            dbContext.MemoryParticipants.Attach(participant);
            dbContext.Entry(participant).State = EntityState.Modified;
        }
        else
        {
            await dbContext.MemoryParticipants.AddAsync(
                participant,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        MemoryInvitation invitation,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        dbContext.MemoryInvitations.Attach(invitation);
        dbContext.Entry(invitation).State = EntityState.Modified;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ExpirePendingInvitationsAsync(
    Guid? memoryId = null,
    Guid? userId = null,
    CancellationToken cancellationToken = default)
    {
        await using var dbContext =
            await _contextFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var query = dbContext.MemoryInvitations
            .Where(x =>
                x.Status == InvitationStatus.Pending &&
                x.ExpiresAtUtc <= now &&
                x.DeletedAtUtc == null);

        if (memoryId.HasValue)
            query = query.Where(x => x.MemoryId == memoryId.Value);

        if (userId.HasValue)
            query = query.Where(x => x.InvitedUserId == userId.Value);

        var expiredInvitations = await query.ToListAsync(cancellationToken);

        foreach (var invitation in expiredInvitations)
            invitation.Expire();

        if (expiredInvitations.Count > 0)
            await dbContext.SaveChangesAsync(cancellationToken);

        return expiredInvitations.Count;
    }
}
