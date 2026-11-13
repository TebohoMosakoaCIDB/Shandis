using Shandis.Application.Abstractions.Persistence;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Memories;

public class MemoryService : IMemoryService
{
    private readonly IMemoryRepository _memoryRepository;

    public MemoryService(IMemoryRepository memoryRepository)
    {
        _memoryRepository = memoryRepository;
    }

    public async Task<CreateMemoryResult> CreateAsync(
        CreateMemoryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CategoryId.HasValue)
        {
            var categoryExists = await _memoryRepository.CategoryExistsAsync(
                command.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                throw new InvalidOperationException(
                    "The selected Memory category does not exist or is inactive.");
            }
        }

        if (command.PlaceId.HasValue)
        {
            var placeExists = await _memoryRepository.PlaceExistsAsync(
                command.PlaceId.Value,
                cancellationToken);

            if (!placeExists)
            {
                throw new InvalidOperationException(
                    "The selected place does not exist.");
            }
        }

        var memory = new Memory(
            title: command.Title,
            ownerId: command.OwnerId,
            visibility: command.Visibility,
            contributionPolicy: command.ContributionPolicy,
            timeZoneId: command.TimeZoneId,
            description: command.Description,
            categoryId: command.CategoryId,
            placeId: command.PlaceId,
            startDateUtc: command.StartDateUtc,
            endDateUtc: command.EndDateUtc);

        var ownerParticipant = new MemoryParticipant(
            memoryId: memory.Id,
            userId: command.OwnerId,
            role: ParticipantRole.Owner,
            joinSource: ParticipantJoinSource.Owner);

        await _memoryRepository.AddAsync(
            memory,
            ownerParticipant,
            cancellationToken);

        return new CreateMemoryResult
        {
            Id = memory.Id,
            Title = memory.Title,
            Description = memory.Description,
            OwnerId = memory.OwnerId,
            CategoryId = memory.CategoryId,
            PlaceId = memory.PlaceId,
            Visibility = memory.Visibility,
            ContributionPolicy = memory.ContributionPolicy,
            Status = memory.Status,
            CreatedAtUtc = memory.CreatedAtUtc
        };
    }

    public async Task<GetMemoryResult?> GetByIdAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var memory = await _memoryRepository.GetByIdAsync(
            memoryId,
            cancellationToken);

        if (memory is null)
            return null;

        var isActiveParticipant =
            await _memoryRepository.IsActiveParticipantAsync(
                memoryId,
                requestingUserId,
                cancellationToken);

        var isPubliclyVisible =
            memory.Visibility == MemoryVisibility.Public &&
            memory.Status is MemoryStatus.Published or MemoryStatus.Completed;

        if (!isActiveParticipant && !isPubliclyVisible)
            return null;

        return new GetMemoryResult
        {
            Id = memory.Id,
            Title = memory.Title,
            Description = memory.Description,
            OwnerId = memory.OwnerId,
            CategoryId = memory.CategoryId,
            CategoryName = memory.Category?.Name,
            PlaceId = memory.PlaceId,
            PlaceName = memory.Place?.Name,
            PlaceAddress = memory.Place?.Address,
            Visibility = memory.Visibility,
            ContributionPolicy = memory.ContributionPolicy,
            Status = memory.Status,
            StartDateUtc = memory.StartDateUtc,
            EndDateUtc = memory.EndDateUtc,
            TimeZoneId = memory.TimeZoneId,
            CoverMediaId = memory.CoverMediaId,
            CreatedAtUtc = memory.CreatedAtUtc,
            PublishedAtUtc = memory.PublishedAtUtc
        };
    }

    public async Task<UpdateMemoryResult?> UpdateAsync(
        UpdateMemoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var memory = await _memoryRepository.GetTrackedByIdAsync(
            command.MemoryId,
            cancellationToken);

        if (memory is null)
            return null;

        if (memory.OwnerId != command.RequestingUserId)
            return null;

        if (command.CategoryId.HasValue)
        {
            var categoryExists = await _memoryRepository.CategoryExistsAsync(
                command.CategoryId.Value,
                cancellationToken);

            if (!categoryExists)
            {
                throw new InvalidOperationException(
                    "The selected Memory category does not exist or is inactive.");
            }
        }

        if (command.PlaceId.HasValue)
        {
            var placeExists = await _memoryRepository.PlaceExistsAsync(
                command.PlaceId.Value,
                cancellationToken);

            if (!placeExists)
            {
                throw new InvalidOperationException(
                    "The selected place does not exist.");
            }
        }

        memory.UpdateDetails(
            title: command.Title,
            description: command.Description,
            categoryId: command.CategoryId,
            placeId: command.PlaceId,
            startDateUtc: command.StartDateUtc,
            endDateUtc: command.EndDateUtc);

        memory.UpdateTimeZone(command.TimeZoneId);

        await _memoryRepository.SaveChangesAsync(cancellationToken);

        return new UpdateMemoryResult
        {
            Id = memory.Id,
            Title = memory.Title,
            Description = memory.Description,
            OwnerId = memory.OwnerId,
            CategoryId = memory.CategoryId,
            CategoryName = memory.Category?.Name,
            PlaceId = memory.PlaceId,
            PlaceName = memory.Place?.Name,
            PlaceAddress = memory.Place?.Address,
            Visibility = memory.Visibility,
            ContributionPolicy = memory.ContributionPolicy,
            Status = memory.Status,
            StartDateUtc = memory.StartDateUtc,
            EndDateUtc = memory.EndDateUtc,
            TimeZoneId = memory.TimeZoneId,
            CoverMediaId = memory.CoverMediaId,
            CreatedAtUtc = memory.CreatedAtUtc,
            UpdatedAtUtc = memory.UpdatedAtUtc,
            PublishedAtUtc = memory.PublishedAtUtc
        };
    }

    public async Task<bool> ArchiveAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var memory = await _memoryRepository.GetTrackedByIdAsync(
            memoryId,
            cancellationToken);

        if (memory is null || memory.OwnerId != requestingUserId)
            return false;

        memory.Archive();

        await _memoryRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var memory = await _memoryRepository.GetTrackedByIdAsync(
            memoryId,
            cancellationToken);

        if (memory is null || memory.OwnerId != requestingUserId)
            return false;

        memory.MarkAsDeleted();

        await _memoryRepository.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<MyMemoriesResult> GetMyMemoriesAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(userId));

        if (page < 1)
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "Page must be greater than zero.");

        if (pageSize < 1 || pageSize > 50)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between 1 and 50.");

        var (memories, totalCount) =
            await _memoryRepository.GetMyMemoriesAsync(
                userId,
                page,
                pageSize,
                cancellationToken);

        var items = memories.Select(memory => new MyMemoryItemResult
        {
            Id = memory.Id,
            Title = memory.Title,
            Description = memory.Description,
            OwnerId = memory.OwnerId,
            IsOwner = memory.OwnerId == userId,
            CategoryId = memory.CategoryId,
            CategoryName = memory.Category?.Name,
            PlaceId = memory.PlaceId,
            PlaceName = memory.Place?.Name,
            Visibility = memory.Visibility,
            ContributionPolicy = memory.ContributionPolicy,
            Status = memory.Status,
            StartDateUtc = memory.StartDateUtc,
            EndDateUtc = memory.EndDateUtc,
            TimeZoneId = memory.TimeZoneId,
            CoverMediaId = memory.CoverMediaId,
            CreatedAtUtc = memory.CreatedAtUtc,
            PublishedAtUtc = memory.PublishedAtUtc
        }).ToList();

        return new MyMemoriesResult
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}