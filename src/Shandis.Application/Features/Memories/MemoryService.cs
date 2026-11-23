
using Shandis.Application.Abstractions.Persistence;
using Shandis.Application.Abstractions.Storage;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Memories;

public class MemoryService : IMemoryService
{
    private const long MaxCoverImageSizeBytes = 10 * 1024 * 1024;

    private readonly IMemoryRepository _memoryRepository;
    private readonly IMediaStorageService? _mediaStorageService;

    public MemoryService(IMemoryRepository memoryRepository, IMediaStorageService mediaStorageService)
    {
        _memoryRepository = memoryRepository;
        _mediaStorageService = mediaStorageService;
    }

    // Retained for unit tests and consumers that only exercise non-storage operations.
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
        var memory = await _memoryRepository.GetByIdAsync(
            memoryId,
            cancellationToken);

        if (memory is null)
            return null;

        var isOwner = memory.OwnerId == requestingUserId;
        var participant = memory.Participants.FirstOrDefault(x =>
            x.UserId == requestingUserId &&
            x.Status == ParticipantStatus.Active &&
            x.DeletedAtUtc == null);

        var isActiveParticipant = participant is not null;

        var isPubliclyVisible =
            memory.Visibility == MemoryVisibility.Public &&
            memory.Status is MemoryStatus.Published or MemoryStatus.Completed;

        if (!isOwner && !isActiveParticipant && !isPubliclyVisible)
            return null;

        return new GetMemoryResult
        {
            Id = memory.Id,
            Title = memory.Title,
            Description = memory.Description,
            OwnerId = memory.OwnerId,
            MyRole = isOwner ? ParticipantRole.Owner : participant?.Role,
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
            CoverImageUrl = await GetCoverImageUrlAsync(memory.CoverMedia, cancellationToken),
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
            CoverImageUrl = await GetCoverImageUrlAsync(memory.CoverMedia, cancellationToken),
            CreatedAtUtc = memory.CreatedAtUtc,
            UpdatedAtUtc = memory.UpdatedAtUtc,
            PublishedAtUtc = memory.PublishedAtUtc
        };
    }

    public async Task<bool> SetCoverImageAsync(
        Guid memoryId, Guid ownerId, string fileName, string contentType,
        long fileSizeBytes, Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        if (memoryId == Guid.Empty || ownerId == Guid.Empty)
            throw new ArgumentException("Valid Memory and owner IDs are required.");
        if (fileStream is null || !fileStream.CanRead)
            throw new ArgumentException("A readable image stream is required.", nameof(fileStream));
        if (fileSizeBytes <= 0 || fileSizeBytes > MaxCoverImageSizeBytes)
            throw new InvalidOperationException("A cover image must be larger than zero and no larger than 10 MB.");

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var valid = (contentType.ToLowerInvariant(), extension) switch
        {
            ("image/jpeg", ".jpg" or ".jpeg") => true,
            ("image/png", ".png") => true,
            ("image/webp", ".webp") => true,
            _ => false
        };
        if (!valid)
            throw new InvalidOperationException("Cover images must be JPG, PNG or WebP files.");

        var memory = await _memoryRepository.GetByIdAsync(memoryId, cancellationToken);
        if (memory is null || memory.OwnerId != ownerId)
            return false;

        var storage = _mediaStorageService
            ?? throw new InvalidOperationException("Media storage is not configured.");

        var mediaId = Guid.NewGuid();
        var objectPath = $"memories/{memoryId}/cover/{mediaId}{extension}";
        await storage.UploadAsync(objectPath, fileStream, contentType, cancellationToken);

        try
        {
            var media = new MemoryMedia(
                memoryId, ownerId, fileName, objectPath, contentType, fileSizeBytes);
            var result = await _memoryRepository.SetCoverMediaAsync(
                memoryId, ownerId, media, cancellationToken);

            if (!result.Updated)
            {
                await TryDeleteStorageObjectAsync(objectPath);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(result.PreviousBlobName))
                await TryDeleteStorageObjectAsync(result.PreviousBlobName);

            return true;
        }
        catch
        {
            await TryDeleteStorageObjectAsync(objectPath);
            throw;
        }
    }

    public async Task<bool> RemoveCoverImageAsync(
        Guid memoryId, Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        if (memoryId == Guid.Empty || ownerId == Guid.Empty)
            throw new ArgumentException("Valid Memory and owner IDs are required.");

        var result = await _memoryRepository.RemoveCoverMediaAsync(
            memoryId, ownerId, cancellationToken);

        if (!result.Removed)
            return false;

        if (!string.IsNullOrWhiteSpace(result.PreviousBlobName))
            await TryDeleteStorageObjectAsync(result.PreviousBlobName);

        return true;
    }

    private async Task<string?> GetCoverImageUrlAsync(
        MemoryMedia? media, CancellationToken cancellationToken)
    {
        if (media is null || _mediaStorageService is null)
            return null;

        try
        {
            return await _mediaStorageService.CreateSignedUrlAsync(
                media.BlobName, 3600, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private async Task TryDeleteStorageObjectAsync(string objectPath)
    {
        try
        {
            if (_mediaStorageService is not null)
                await _mediaStorageService.DeleteAsync(objectPath);
        }
        catch
        {
            // Storage cleanup failures do not undo a successfully saved Memory cover.
        }
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

        var items = await Task.WhenAll(memories.Select(async memory => new MyMemoryItemResult
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
            CoverImageUrl = await GetCoverImageUrlAsync(memory.CoverMedia, cancellationToken),
            CreatedAtUtc = memory.CreatedAtUtc,
            PublishedAtUtc = memory.PublishedAtUtc
        }));

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

        var items = await Task.WhenAll(memories.Select(async memory =>
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
                CoverImageUrl = await GetCoverImageUrlAsync(memory.CoverMedia, cancellationToken),
                CreatedAtUtc = memory.CreatedAtUtc,
                PublishedAtUtc = memory.PublishedAtUtc
            };
        }));

        return new SharedMemoriesResult
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

}
