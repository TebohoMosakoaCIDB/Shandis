using Shandis.Domain.Entities;

namespace Shandis.Application.Abstractions.Persistence;

public interface IMemoryRepository
{
    Task<bool> CategoryExistsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> PlaceExistsAsync(
        Guid placeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Memory memory,
        MemoryParticipant ownerParticipant,
        CancellationToken cancellationToken = default);

    Task<Memory?> GetByIdAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default);

    Task<Memory?> GetTrackedByIdAsync(
        Guid memoryId,
        CancellationToken cancellationToken = default);

    Task<bool> IsActiveParticipantAsync(
        Guid memoryId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Memory> Items, int TotalCount)> GetMyMemoriesAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}