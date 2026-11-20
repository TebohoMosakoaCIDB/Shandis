using Shandis.Application.Abstractions.Persistence;
using Shandis.Domain.Entities;

namespace Shandis.Application.Features.Memories;

public sealed class SavedMemoryService(ISavedMemoryRepository repository) : ISavedMemoryService
{
    public async Task<bool> SaveAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default)
    {
        ValidateIds(memoryId, userId);
        if (!await repository.CanViewMemoryAsync(memoryId, userId, cancellationToken)) return false;
        if (await repository.IsSavedAsync(memoryId, userId, cancellationToken)) return true;
        await repository.AddAsync(new SavedMemory(userId, memoryId), cancellationToken);
        return true;
    }

    public async Task<bool> UnsaveAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default)
    {
        ValidateIds(memoryId, userId);
        await repository.RemoveAsync(memoryId, userId, cancellationToken);
        return true;
    }

    public Task<bool> IsSavedAsync(Guid memoryId, Guid userId, CancellationToken cancellationToken = default)
    {
        ValidateIds(memoryId, userId);
        return repository.IsSavedAsync(memoryId, userId, cancellationToken);
    }

    public async Task<SavedMemoriesResult> GetSavedAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A valid user ID is required.", nameof(userId));
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize is < 1 or > 50) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var (memories, totalCount) = await repository.GetSavedAsync(userId, page, pageSize, cancellationToken);
        return new SavedMemoriesResult
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = memories.Select(memory => new SavedMemoryItemResult
            {
                Id = memory.Id,
                Title = memory.Title,
                Description = memory.Description,
                CategoryName = memory.Category?.Name,
                PlaceName = memory.Place?.Name,
                Visibility = (int)memory.Visibility,
                StartDateUtc = memory.StartDateUtc,
                SavedAtUtc = memory.CreatedAtUtc
            }).ToList()
        };
    }

    private static void ValidateIds(Guid memoryId, Guid userId)
    {
        if (memoryId == Guid.Empty) throw new ArgumentException("A valid Memory ID is required.", nameof(memoryId));
        if (userId == Guid.Empty) throw new ArgumentException("A valid user ID is required.", nameof(userId));
    }
}
