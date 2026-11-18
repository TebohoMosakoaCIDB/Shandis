using Shandis.Application.Features.Moments.Results;
using Shandis.Domain.Entities;

namespace Shandis.Application.Abstractions.Persistence;

public interface IMomentRepository
{
    Task<MomentContributionContext?> GetContributionContextAsync(
        Guid memoryId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        MemoryMoment moment,
        MemoryParticipant? participant,
        bool isNewParticipant,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<MemoryMoment> Moments, int TotalCount)> GetByMemoryAsync(
        Guid memoryId,
        Guid userId,
        Guid memoryOwnerId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<MemoryMoment?> GetByIdAsync(
        Guid momentId,
        CancellationToken cancellationToken = default);

    Task SaveModerationAsync(
        MemoryMoment moment,
        MemoryParticipant? participant,
        bool isNewParticipant,
        CancellationToken cancellationToken = default);
}