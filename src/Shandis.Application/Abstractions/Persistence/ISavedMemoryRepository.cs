using Shandis.Domain.Entities;

namespace Shandis.Application.Abstractions.Persistence;

public interface ISavedMemoryRepository
{
    Task<bool> CanViewMemoryAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsSavedAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(SavedMemory savedMemory, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Memory> Items, int TotalCount)> GetSavedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
}
