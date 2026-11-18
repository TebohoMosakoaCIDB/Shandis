using Shandis.Application.Features.Moments.Commands;
using Shandis.Application.Features.Moments.Results;

namespace Shandis.Application.Features.Moments;

public interface IMomentService
{
    Task<CreateMomentResult> CreateAsync(
        CreateMomentCommand command,
        CancellationToken cancellationToken = default);

    Task<GetMomentsResult> GetByMemoryAsync(
        Guid memoryId,
        Guid userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<CreateMomentResult> GetByIdAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CreateMomentResult> ApproveAsync(
    Guid momentId,
    Guid userId,
    CancellationToken cancellationToken = default);

    Task<CreateMomentResult> RejectAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MomentLikeResult> LikeAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MomentLikeResult> UnlikeAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<MomentCommentResult> CreateCommentAsync(
        Guid momentId, Guid userId, string content, Guid? parentCommentId = null,
        CancellationToken cancellationToken = default);
    Task<GetMomentCommentsResult> GetCommentsAsync(
        Guid momentId, Guid userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);
    Task<MomentCommentResult> UpdateCommentAsync(
        Guid momentId, Guid commentId, Guid userId, string content,
        CancellationToken cancellationToken = default);
    Task DeleteCommentAsync(
        Guid momentId, Guid commentId, Guid userId,
        CancellationToken cancellationToken = default);
    Task<MomentCommentLikeResult> LikeCommentAsync(
        Guid momentId, Guid commentId, Guid userId,
        CancellationToken cancellationToken = default);
    Task<MomentCommentLikeResult> UnlikeCommentAsync(
        Guid momentId, Guid commentId, Guid userId,
        CancellationToken cancellationToken = default);
}
