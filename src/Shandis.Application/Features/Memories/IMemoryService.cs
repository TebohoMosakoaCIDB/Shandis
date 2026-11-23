namespace Shandis.Application.Features.Memories;

public interface IMemoryService
{
    Task<CreateMemoryResult> CreateAsync(
        CreateMemoryCommand command,
        CancellationToken cancellationToken = default);

    Task<GetMemoryResult?> GetByIdAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    Task<UpdateMemoryResult?> UpdateAsync(
        UpdateMemoryCommand command,
        CancellationToken cancellationToken = default);

    Task<bool> SetCoverImageAsync(
        Guid memoryId, Guid ownerId, string fileName, string contentType,
        long fileSizeBytes, Stream fileStream,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveCoverImageAsync(
        Guid memoryId, Guid ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> PublishAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default);

    Task<MyMemoriesResult> GetMyMemoriesAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SharedMemoriesResult> GetSharedWithMeAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}