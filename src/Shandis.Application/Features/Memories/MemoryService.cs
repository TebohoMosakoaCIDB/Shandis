
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
        var categoryTask = command.CategoryId.HasValue
            ? _memoryRepository.CategoryExistsAsync(
                command.CategoryId.Value,
                cancellationToken)
            : Task.FromResult(true);

        var placeTask = command.PlaceId.HasValue
            ? _memoryRepository.PlaceExistsAsync(
                command.PlaceId.Value,
                cancellationToken)
            : Task.FromResult(true);

        await Task.WhenAll(categoryTask, placeTask);

        if (!await categoryTask)
        {
            throw new InvalidOperationException(
                "The selected Memory category does not exist or is inactive.");
        }

        if (!await placeTask)
        {
            throw new InvalidOperationException(
                "The selected place does not exist.");
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
        var memoryTask = _memoryRepository.GetByIdAsync(
            memoryId,
            cancellationToken);

        var participantTask = _memoryRepository.IsActiveParticipantAsync(
            memoryId,
            requestingUserId,
            cancellationToken);

        await Task.WhenAll(memoryTask, participantTask);

        var memory = await memoryTask;
        var isActiveParticipant = await participantTask;

        if (memory is null)
            return null;

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
        var memory = await _memoryRepository.GetByIdAsync(
            command.MemoryId,
            cancellationToken);

        if (memory is null || memory.OwnerId != command.RequestingUserId)
            return null;

        var categoryTask = command.CategoryId.HasValue
            ? _memoryRepository.CategoryExistsAsync(
                command.CategoryId.Value,
                cancellationToken)
            : Task.FromResult(true);

        var placeTask = command.PlaceId.HasValue
            ? _memoryRepository.PlaceExistsAsync(
                command.PlaceId.Value,
                cancellationToken)
            : Task.FromResult(true);

        await Task.WhenAll(categoryTask, placeTask);

        if (!await categoryTask)
        {
            throw new InvalidOperationException(
                "The selected Memory category does not exist or is inactive.");
        }

        if (!await placeTask)
        {
            throw new InvalidOperationException(
                "The selected place does not exist.");
        }

        memory.UpdateDetails(
            title: command.Title,
            description: command.Description,
            categoryId: command.CategoryId,
            placeId: command.PlaceId,
            startDateUtc: command.StartDateUtc,
            endDateUtc: command.EndDateUtc);

        memory.UpdateTimeZone(command.TimeZoneId);

        await _memoryRepository.UpdateAsync(
            memory,
            cancellationToken);

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

    public async Task<bool> PublishAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        if (memoryId == Guid.Empty)
            throw new ArgumentException("A valid Memory ID is required.", nameof(memoryId));

        if (requestingUserId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.", nameof(requestingUserId));

        var memory = await _memoryRepository.GetByIdAsync(
            memoryId,
            cancellationToken);

        if (memory is null || memory.OwnerId != requestingUserId)
            return false;

        memory.Publish();

        await _memoryRepository.UpdateAsync(
            memory,
            cancellationToken);

        return true;
    }

    public async Task<bool> ArchiveAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var memory = await _memoryRepository.GetByIdAsync(
            memoryId,
            cancellationToken);

        if (memory is null || memory.OwnerId != requestingUserId)
            return false;

        memory.Archive();

        await _memoryRepository.UpdateAsync(
            memory,
            cancellationToken);

        return true;
    }

    public async Task<bool> DeleteAsync(
        Guid memoryId,
        Guid requestingUserId,
        CancellationToken cancellationToken = default)
    {
        var memory = await _memoryRepository.GetByIdAsync(
            memoryId,
            cancellationToken);

        if (memory is null || memory.OwnerId != requestingUserId)
            return false;

        memory.MarkAsDeleted();

        await _memoryRepository.UpdateAsync(
            memory,
            cancellationToken);

        return true;
    }

    public async Task<MyMemoriesResult> GetMyMemoriesAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "A valid user ID is required.",
                nameof(userId));

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


    public async Task<SharedMemoriesResult> GetSharedWithMeAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException(
                "A valid user ID is required.",
                nameof(userId));

        if (page < 1)
            throw new ArgumentOutOfRangeException(
                nameof(page),
                "Page must be greater than zero.");

        if (pageSize < 1 || pageSize > 50)
            throw new ArgumentOutOfRangeException(
                nameof(pageSize),
                "Page size must be between 1 and 50.");

        var (memories, totalCount) =
            await _memoryRepository.GetSharedWithMeAsync(
                userId,
                page,
                pageSize,
                cancellationToken);

        var items = memories.Select(memory =>
        {
            var participant = memory.Participants
                .First(x =>
                    x.UserId == userId &&
                    x.Role != ParticipantRole.Owner &&
                    x.JoinSource == ParticipantJoinSource.Invitation &&
                    x.Status == ParticipantStatus.Active &&
                    x.DeletedAtUtc == null);

            return new SharedMemoryItemResult
            {
                Id = memory.Id,
                Title = memory.Title,
                Description = memory.Description,
                OwnerId = memory.OwnerId,
                CategoryId = memory.CategoryId,
                CategoryName = memory.Category?.Name,
                PlaceId = memory.PlaceId,
                PlaceName = memory.Place?.Name,
                Visibility = memory.Visibility,
                ContributionPolicy = memory.ContributionPolicy,
                Status = memory.Status,
                MyRole = participant.Role,
                StartDateUtc = memory.StartDateUtc,
                EndDateUtc = memory.EndDateUtc,
                TimeZoneId = memory.TimeZoneId,
                CoverMediaId = memory.CoverMediaId,
                CreatedAtUtc = memory.CreatedAtUtc,
                PublishedAtUtc = memory.PublishedAtUtc
            };
        }).ToList();

        return new SharedMemoriesResult
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

}
