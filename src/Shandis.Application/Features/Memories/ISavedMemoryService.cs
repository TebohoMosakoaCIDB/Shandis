namespace Shandis.Application.Features.Memories;

public interface ISavedMemoryService
{
    Task<bool> SaveAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UnsaveAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsSavedAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default);
    Task<SavedMemoriesResult> GetSavedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
