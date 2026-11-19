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

    Task<IReadOnlyDictionary<Guid, int>> GetCommentCountsAsync(
        IReadOnlyCollection<Guid> momentIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetActiveParticipantUserIdsAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default);

    Task<MemoryMoment?> GetByIdAsync(
        Guid momentId,
        CancellationToken cancellationToken = default);

    Task SaveModerationAsync(
        MemoryMoment moment,
        MemoryParticipant? participant,
        bool isNewParticipant,
        CancellationToken cancellationToken = default);

    Task SetLikeAsync(
        Guid momentId,
        Guid userId,
        bool isLiked,
        CancellationToken cancellationToken = default);

    Task<MomentLikeResult> GetLikeSummaryAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddCommentAsync(MomentComment comment, CancellationToken cancellationToken = default);
    Task<MomentComment?> GetCommentByIdAsync(Guid commentId, CancellationToken cancellationToken = default);
    Task SaveCommentAsync(MomentComment comment, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<MomentCommentResult> Comments, int TotalCount)> GetCommentsAsync(
        Guid momentId, Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MomentCommentResult?> GetCommentResultAsync(
        Guid commentId, Guid userId, CancellationToken cancellationToken = default);
    Task SetCommentLikeAsync(
        Guid commentId, Guid userId, bool isLiked, CancellationToken cancellationToken = default);
    Task<MomentCommentLikeResult> GetCommentLikeSummaryAsync(
        Guid commentId, Guid userId, CancellationToken cancellationToken = default);
}
