using Shandis.Application.Abstractions.Persistence;
using Shandis.Application.Abstractions.Storage;
using Shandis.Application.Features.Notifications;
using Shandis.Application.Features.Moments.Commands;
using Shandis.Application.Features.Moments.Results;
using Shandis.Domain.Entities;
using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Moments;

public class MomentService : IMomentService
{
    private const int MaxMediaFiles = 10;
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    private readonly IMomentRepository _momentRepository;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly INotificationService _notificationService;

    public MomentService(
        IMomentRepository momentRepository,
        IMediaStorageService mediaStorageService,
        INotificationService notificationService)
    {
        _momentRepository = momentRepository;
        _mediaStorageService = mediaStorageService;
        _notificationService = notificationService;
    }

    public async Task<CreateMomentResult> CreateAsync(
        CreateMomentCommand command,
        CancellationToken cancellationToken = default)
    {
        ValidateCommand(command);

        var contributionContext =
            await _momentRepository.GetContributionContextAsync(
                command.MemoryId,
                command.CreatedByUserId,
                cancellationToken);

        if (contributionContext is null)
            throw new InvalidOperationException("Memory was not found.");

        var memory = contributionContext.Memory;
        var participant = contributionContext.Participant;

        var isOwner = memory.OwnerId == command.CreatedByUserId;

        var requiresApproval = ValidateContributionPermission(
            memory,
            participant,
            isOwner);

        var sourceType = isOwner
            ? MomentSourceType.Owner
            : MomentSourceType.Community;

        var moment = new MemoryMoment(
            memoryId: memory.Id,
            createdByUserId: command.CreatedByUserId,
            momentType: command.MomentType,
            sourceType: sourceType,
            title: command.Title,
            content: command.Content,
            happenedAtUtc: command.HappenedAtUtc,
            requiresApproval: requiresApproval);

        var participantToSave = GetParticipantToSave(
            memory,
            participant,
            command.CreatedByUserId,
            isOwner,
            requiresApproval);

        var isNewParticipant =
            participantToSave is not null &&
            participant is null;

        var uploadedObjectPaths = new List<string>();

        try
        {
            for (var index = 0; index < command.Media.Count; index++)
            {
                var upload = command.Media[index];

                var mediaType = GetMediaType(upload.ContentType);
                var extension = Path.GetExtension(upload.FileName);

                var mediaId = Guid.NewGuid();

                var objectPath =
                    $"memories/{memory.Id}/moments/{moment.Id}/{mediaId}{extension}";

                await _mediaStorageService.UploadAsync(
                    objectPath,
                    upload.FileStream,
                    upload.ContentType,
                    cancellationToken);

                uploadedObjectPaths.Add(objectPath);

                var media = new MomentMedia(
                    momentId: moment.Id,
                    uploadedByUserId: command.CreatedByUserId,
                    fileName: Path.GetFileName(upload.FileName),
                    blobName: objectPath,
                    contentType: upload.ContentType,
                    fileSizeBytes: upload.FileSizeBytes,
                    mediaType: mediaType,
                    sortOrder: index);

                moment.AddMedia(media);
            }

            await _momentRepository.AddAsync(
                moment,
                participantToSave,
                isNewParticipant,
                cancellationToken);

        }
        catch
        {
            await CleanupUploadedFilesAsync(uploadedObjectPaths);
            throw;
        }

        if (requiresApproval && memory.OwnerId != command.CreatedByUserId)
        {
            await _notificationService.CreateAsync(
                memory.OwnerId,
                command.CreatedByUserId,
                NotificationType.MomentPendingApproval,
                "Moment awaiting approval",
                $"A new contribution is waiting for approval in \"{memory.Title}\".",
                $"/memories/{memory.Id}",
                memory.Id,
                moment.Id,
                cancellationToken);
        }

        return await MapResultAsync(moment, cancellationToken);
    }

    private static bool ValidateContributionPermission(
        Memory memory,
        MemoryParticipant? participant,
        bool isOwner)
    {
        if (memory.DeletedAtUtc is not null)
            throw new InvalidOperationException(
                "This Memory is no longer available.");

        if (memory.Status == MemoryStatus.Archived)
            throw new InvalidOperationException(
                "You cannot contribute to an archived Memory.");

        if (memory.Status == MemoryStatus.Draft && !isOwner)
            throw new InvalidOperationException(
                "Only the owner can contribute to a draft Memory.");

        if (isOwner)
            return false;

        // An explicitly invited Viewer must never contribute, even when
        // the Memory is public and its contribution policy is Open or ApprovalRequired.
        if (participant is
            {
                Status: ParticipantStatus.Active,
                Role: ParticipantRole.Viewer
            })
        {
            throw new UnauthorizedAccessException(
                "Viewers can view this Memory but cannot add Moments.");
        }

        var isActiveContributor =
            participant is not null &&
            participant.Status == ParticipantStatus.Active &&
            participant.Role == ParticipantRole.Contributor;

        if (memory.Visibility == MemoryVisibility.Private)
        {
            if (!isActiveContributor)
                throw new UnauthorizedAccessException(
                    "Only the owner and accepted Contributors can contribute to a private Memory.");

            return false;
        }

        if (memory.Visibility != MemoryVisibility.Public)
            throw new InvalidOperationException(
                "The Memory visibility is invalid.");

        switch (memory.ContributionPolicy)
        {
            case MemoryContributionPolicy.Open:
                return false;

            case MemoryContributionPolicy.ApprovalRequired:
                return true;

            case MemoryContributionPolicy.InvitationOnly:
                if (!isActiveContributor)
                    throw new UnauthorizedAccessException(
                        "You must be an active Contributor to contribute to this Memory.");

                return false;

            default:
                throw new InvalidOperationException(
                    "The Memory contribution policy is invalid.");
        }
    }

    private static MemoryParticipant? GetParticipantToSave(
        Memory memory,
        MemoryParticipant? participant,
        Guid userId,
        bool isOwner,
        bool requiresApproval)
    {
        if (isOwner || requiresApproval)
            return null;

        // Private Memories and InvitationOnly Memories require an
        // existing active Contributor. No participant change is needed.
        if (memory.Visibility == MemoryVisibility.Private ||
            memory.ContributionPolicy == MemoryContributionPolicy.InvitationOnly)
        {
            return null;
        }

        if (participant is null)
        {
            return new MemoryParticipant(
                memory.Id,
                userId,
                ParticipantRole.Contributor,
                ParticipantJoinSource.Contribution);
        }

        if (participant.Status == ParticipantStatus.Removed)
        {
            participant.Reactivate(
                ParticipantRole.Contributor,
                ParticipantJoinSource.Contribution);

            return participant;
        }

        return null;
    }

    private static void ValidateCommand(CreateMomentCommand command)
    {
        if (command.MemoryId == Guid.Empty)
            throw new ArgumentException("A valid Memory ID is required.");

        if (command.CreatedByUserId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.");

        if (!Enum.IsDefined(command.MomentType))
            throw new ArgumentException("The Moment type is invalid.");

        if (command.Media.Count > MaxMediaFiles)
            throw new InvalidOperationException(
                $"A Moment cannot contain more than {MaxMediaFiles} media files.");

        if (command.Media.Any(x => x.FileStream is null || !x.FileStream.CanRead))
            throw new ArgumentException(
                "All uploaded media must have readable file streams.");

        foreach (var upload in command.Media)
        {
            if (string.IsNullOrWhiteSpace(upload.FileName))
                throw new ArgumentException("A media filename is required.");

            if (string.IsNullOrWhiteSpace(upload.ContentType))
                throw new ArgumentException("A media content type is required.");

            if (upload.FileSizeBytes <= 0 ||
                upload.FileSizeBytes > MaxFileSizeBytes)
            {
                throw new InvalidOperationException(
                    "Each media file must be larger than zero and no larger than 50 MB.");
            }

            ValidateMediaFile(upload);
        }

        var mediaTypes = command.Media
            .Select(x => GetMediaType(x.ContentType))
            .ToList();

        switch (command.MomentType)
        {
            case MomentType.Photo:
                if (!mediaTypes.Contains(MediaType.Image) ||
                    mediaTypes.Any(x => x != MediaType.Image))
                {
                    throw new InvalidOperationException(
                        "A Photo Moment must contain image files only.");
                }
                break;

            case MomentType.Video:
                if (!mediaTypes.Contains(MediaType.Video) ||
                    mediaTypes.Any(x => x != MediaType.Video))
                {
                    throw new InvalidOperationException(
                        "A Video Moment must contain video files only.");
                }
                break;

            case MomentType.Audio:
                if (!mediaTypes.Contains(MediaType.Audio) ||
                    mediaTypes.Any(x => x != MediaType.Audio))
                {
                    throw new InvalidOperationException(
                        "An Audio Moment must contain audio files only.");
                }
                break;

            case MomentType.Story:
                if (string.IsNullOrWhiteSpace(command.Content))
                    throw new InvalidOperationException(
                        "A Story Moment must contain text.");

                if (command.Media.Count > 0)
                    throw new InvalidOperationException(
                        "A Story Moment cannot contain media files.");
                break;

            case MomentType.Mixed:
                if (string.IsNullOrWhiteSpace(command.Content) &&
                    command.Media.Count == 0)
                {
                    throw new InvalidOperationException(
                        "A Mixed Moment must contain text or media.");
                }
                break;
        }
    }

    private static void ValidateMediaFile(MomentMediaUpload upload)
    {
        var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
        var contentType = upload.ContentType.ToLowerInvariant();

        var valid = contentType switch
        {
            "image/jpeg" => extension is ".jpg" or ".jpeg",
            "image/png" => extension == ".png",
            "image/webp" => extension == ".webp",
            "image/gif" => extension == ".gif",

            "video/mp4" => extension == ".mp4",
            "video/webm" => extension == ".webm",
            "video/quicktime" => extension == ".mov",

            "audio/mpeg" => extension == ".mp3",
            "audio/wav" => extension is ".wav" or ".wave",
            "audio/ogg" => extension == ".ogg",
            "audio/mp4" => extension == ".m4a",

            _ => false
        };

        if (!valid)
            throw new InvalidOperationException(
                $"The file type '{upload.ContentType}' is not supported or does not match its extension.");
    }

    private static MediaType GetMediaType(string contentType)
    {
        if (contentType.StartsWith(
                "image/",
                StringComparison.OrdinalIgnoreCase))
        {
            return MediaType.Image;
        }

        if (contentType.StartsWith(
                "video/",
                StringComparison.OrdinalIgnoreCase))
        {
            return MediaType.Video;
        }

        if (contentType.StartsWith(
                "audio/",
                StringComparison.OrdinalIgnoreCase))
        {
            return MediaType.Audio;
        }

        throw new InvalidOperationException(
            $"Unsupported media type: {contentType}");
    }

    private async Task<CreateMomentResult> MapResultAsync(
        MemoryMoment moment,
        CancellationToken cancellationToken,
        Guid? currentUserId = null)
    {
        var userId = currentUserId ?? moment.CreatedByUserId;

        var likeSummary = await _momentRepository.GetLikeSummaryAsync(
            moment.Id,
            userId,
            cancellationToken);

        var result = new CreateMomentResult
        {
            Id = moment.Id,
            MemoryId = moment.MemoryId,
            CreatedByUserId = moment.CreatedByUserId,
            Title = moment.Title,
            Content = moment.Content,
            MomentType = moment.MomentType,
            SourceType = moment.SourceType,
            Status = moment.Status,
            HappenedAtUtc = moment.HappenedAtUtc,
            CreatedAtUtc = moment.CreatedAtUtc,
            PublishedAtUtc = moment.PublishedAtUtc,
            LikeCount = likeSummary.LikeCount,
            IsLikedByCurrentUser = likeSummary.IsLikedByCurrentUser
        };

        foreach (var media in moment.Media.OrderBy(x => x.SortOrder))
        {
            string signedUrl;

            try
            {
                signedUrl = await _mediaStorageService.CreateSignedUrlAsync(
                    media.BlobName,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                // The Moment has already been saved. A signed URL can
                // be generated again when the Moment is retrieved.
                signedUrl = string.Empty;
            }

            result.Media.Add(new MomentMediaResult
            {
                Id = media.Id,
                FileName = media.FileName,
                ContentType = media.ContentType,
                FileSizeBytes = media.FileSizeBytes,
                MediaType = media.MediaType,
                SortOrder = media.SortOrder,
                Url = signedUrl
            });
        }

        return result;
    }

    private async Task CleanupUploadedFilesAsync(
        IEnumerable<string> uploadedObjectPaths)
    {
        foreach (var objectPath in uploadedObjectPaths)
        {
            try
            {
                await _mediaStorageService.DeleteAsync(objectPath);
            }
            catch
            {
                // Do not hide the original upload/database exception.
                // A production cleanup job can handle failed deletions.
            }
        }
    }

    public async Task<GetMomentsResult> GetByMemoryAsync(
    Guid memoryId,
    Guid userId,
    int page = 1,
    int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        if (memoryId == Guid.Empty)
            throw new ArgumentException("A valid Memory ID is required.");

        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.");

        if (page < 1)
            throw new ArgumentException("Page must be greater than zero.");

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100.");

        var context = await _momentRepository.GetContributionContextAsync(
            memoryId,
            userId,
            cancellationToken);

        if (context is null)
            throw new KeyNotFoundException("Memory was not found.");

        EnsureCanViewMemory(context.Memory, context.Participant, userId);

        var (moments, totalCount) =
            await _momentRepository.GetByMemoryAsync(
                memoryId,
                userId,
                context.Memory.OwnerId,
                page,
                pageSize,
                cancellationToken);

        var result = new GetMomentsResult
        {
            MemoryId = memoryId,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        foreach (var moment in moments)
        {
            result.Moments.Add(
                await MapResultAsync(moment, cancellationToken, userId));
        }

        return result;
    }

    public async Task<CreateMomentResult> GetByIdAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (momentId == Guid.Empty)
            throw new ArgumentException("A valid Moment ID is required.");

        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.");

        var moment = await _momentRepository.GetByIdAsync(
            momentId,
            cancellationToken);

        if (moment is null ||
            moment.Status == MomentStatus.Removed)
        {
            throw new KeyNotFoundException("Moment was not found.");
        }

        var context = await _momentRepository.GetContributionContextAsync(
            moment.MemoryId,
            userId,
            cancellationToken);

        if (context is null)
            throw new KeyNotFoundException("Memory was not found.");

        EnsureCanViewMemory(context.Memory, context.Participant, userId);

        var isOwner = context.Memory.OwnerId == userId;
        var isAuthor = moment.CreatedByUserId == userId;

        if (moment.Status != MomentStatus.Published &&
            !isOwner &&
            !isAuthor)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to view this Moment.");
        }

        return await MapResultAsync(moment, cancellationToken, userId);
    }

    private static void EnsureCanViewMemory(
        Memory memory,
        MemoryParticipant? participant,
        Guid userId)
    {
        if (memory.DeletedAtUtc is not null)
            throw new KeyNotFoundException("Memory was not found.");

        var isOwner = memory.OwnerId == userId;

        var isActiveParticipant =
            participant is not null &&
            participant.Status == ParticipantStatus.Active;

        var isPublicAndPublished =
            memory.Visibility == MemoryVisibility.Public &&
            (memory.Status == MemoryStatus.Published ||
             memory.Status == MemoryStatus.Completed);

        if (!isOwner && !isActiveParticipant && !isPublicAndPublished)
        {
            throw new UnauthorizedAccessException(
                "You are not allowed to view this Memory.");
        }
    }

    public async Task<CreateMomentResult> ApproveAsync(
    Guid momentId,
    Guid userId,
    CancellationToken cancellationToken = default)
    {
        var (moment, participant, isNewParticipant) =
            await GetMomentForModerationAsync(
                momentId,
                userId,
                cancellationToken);

        if (moment.Status != MomentStatus.Pending)
            throw new InvalidOperationException(
                "Only pending Moments can be approved.");

        // A public ApprovalRequired contribution does not become a Memory
        // participant until the owner approves it. Create or reactivate the
        // Contributor participant at the same time as publishing the Moment.
        if (participant is null)
        {
            participant = new MemoryParticipant(
                moment.MemoryId,
                moment.CreatedByUserId,
                ParticipantRole.Contributor,
                ParticipantJoinSource.Contribution);

            isNewParticipant = true;
        }
        else if (participant.Status == ParticipantStatus.Removed)
        {
            participant.Reactivate(
                ParticipantRole.Contributor,
                ParticipantJoinSource.Contribution);

            isNewParticipant = false;
        }
        else if (participant.Role == ParticipantRole.Viewer)
        {
            throw new UnauthorizedAccessException(
                "A Viewer cannot have a Moment approved as a contribution.");
        }

        moment.Approve();

        await _momentRepository.SaveModerationAsync(
            moment,
            participant,
            isNewParticipant,
            cancellationToken);

        await _notificationService.CreateAsync(
            moment.CreatedByUserId,
            userId,
            NotificationType.MomentApproved,
            "Moment approved",
            $"Your contribution to \"{moment.Memory.Title}\" has been approved.",
            $"/memories/{moment.MemoryId}",
            moment.MemoryId,
            moment.Id,
            cancellationToken);

        return await MapResultAsync(moment, cancellationToken, userId);
    }

    public async Task<CreateMomentResult> RejectAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (moment, _, _) = await GetMomentForModerationAsync(
            momentId,
            userId,
            cancellationToken);

        if (moment.Status != MomentStatus.Pending)
            throw new InvalidOperationException(
                "Only pending Moments can be rejected.");

        moment.Reject();

        await _momentRepository.SaveModerationAsync(
            moment,
            null,
            false,
            cancellationToken);

        await _notificationService.CreateAsync(
            moment.CreatedByUserId,
            userId,
            NotificationType.MomentRejected,
            "Moment not approved",
            $"Your contribution to \"{moment.Memory.Title}\" was not approved.",
            $"/memories/{moment.MemoryId}",
            moment.MemoryId,
            moment.Id,
            cancellationToken);

        return await MapResultAsync(moment, cancellationToken, userId);
    }

    private async Task<(
        MemoryMoment Moment,
        MemoryParticipant? Participant,
        bool IsNewParticipant)> GetMomentForModerationAsync(
            Guid momentId,
            Guid userId,
            CancellationToken cancellationToken)
    {
        if (momentId == Guid.Empty)
            throw new ArgumentException("A valid Moment ID is required.");

        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.");

        var moment = await _momentRepository.GetByIdAsync(
            momentId,
            cancellationToken);

        if (moment is null || moment.Status == MomentStatus.Removed)
            throw new KeyNotFoundException("Moment was not found.");

        var context = await _momentRepository.GetContributionContextAsync(
            moment.MemoryId,
            moment.CreatedByUserId,
            cancellationToken);

        if (context is null)
            throw new KeyNotFoundException("Memory was not found.");

        if (context.Memory.OwnerId != userId)
            throw new UnauthorizedAccessException(
                "Only the Memory owner can moderate Moments.");

        if (moment.SourceType == MomentSourceType.Owner)
            throw new InvalidOperationException(
                "The Memory owner cannot moderate their own Moments.");

        var participant = context.Participant;

        return (moment, participant, participant is null);
    }

    public async Task<MomentLikeResult> LikeAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await SetLikeAsync(
            momentId,
            userId,
            true,
            cancellationToken);
    }

    public async Task<MomentLikeResult> UnlikeAsync(
        Guid momentId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await SetLikeAsync(
            momentId,
            userId,
            false,
            cancellationToken);
    }

    private async Task<MomentLikeResult> SetLikeAsync(
        Guid momentId,
        Guid userId,
        bool isLiked,
        CancellationToken cancellationToken)
    {
        if (momentId == Guid.Empty)
            throw new ArgumentException("A valid Moment ID is required.");

        if (userId == Guid.Empty)
            throw new ArgumentException("A valid user ID is required.");

        var moment = await _momentRepository.GetByIdAsync(
            momentId,
            cancellationToken);

        if (moment is null || moment.Status == MomentStatus.Removed)
            throw new KeyNotFoundException("Moment was not found.");

        if (moment.Status != MomentStatus.Published)
            throw new InvalidOperationException(
                "Only published Moments can receive likes.");

        var context = await _momentRepository.GetContributionContextAsync(
            moment.MemoryId,
            userId,
            cancellationToken);

        if (context is null)
            throw new KeyNotFoundException("Memory was not found.");

        EnsureCanViewMemory(
            context.Memory,
            context.Participant,
            userId);

        var previousLike = await _momentRepository.GetLikeSummaryAsync(
            momentId,
            userId,
            cancellationToken);

        await _momentRepository.SetLikeAsync(
            momentId,
            userId,
            isLiked,
            cancellationToken);

        if (isLiked &&
            !previousLike.IsLikedByCurrentUser &&
            moment.CreatedByUserId != userId)
        {
            await _notificationService.CreateAsync(
                moment.CreatedByUserId,
                userId,
                NotificationType.MomentLiked,
                "New like",
                $"Someone liked your Moment in \"{moment.Memory.Title}\".",
                $"/memories/{moment.MemoryId}",
                moment.MemoryId,
                moment.Id,
                cancellationToken);
        }

        return await _momentRepository.GetLikeSummaryAsync(
            momentId,
            userId,
            cancellationToken);
    }


    public async Task<MomentCommentResult> CreateCommentAsync(
        Guid momentId, Guid userId, string content, Guid? parentCommentId = null,
        CancellationToken cancellationToken = default)
    {
        if (momentId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Valid Moment and user IDs are required.");

        var moment = await GetPublishedMomentForInteractionAsync(momentId, userId, cancellationToken);

        if (parentCommentId.HasValue)
        {
            var parent = await _momentRepository.GetCommentByIdAsync(parentCommentId.Value, cancellationToken);
            if (parent is null || parent.MomentId != momentId || parent.ParentCommentId is not null)
                throw new InvalidOperationException("A reply must target an existing top-level comment on this Moment.");
        }

        var comment = new MomentComment(moment.Id, userId, content, parentCommentId);
        await _momentRepository.AddCommentAsync(comment, cancellationToken);

        if (moment.CreatedByUserId != userId)
        {
            await _notificationService.CreateAsync(
                moment.CreatedByUserId,
                userId,
                NotificationType.MomentCommented,
                "New comment",
                $"Someone commented on your Moment in \"{moment.Memory.Title}\".",
                $"/memories/{moment.MemoryId}",
                moment.MemoryId,
                moment.Id,
                cancellationToken);
        }

        return await _momentRepository.GetCommentResultAsync(comment.Id, userId, cancellationToken)
            ?? throw new InvalidOperationException("The comment was saved but could not be retrieved.");
    }

    public async Task<GetMomentCommentsResult> GetCommentsAsync(
        Guid momentId, Guid userId, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (momentId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Valid Moment and user IDs are required.");
        if (page < 1) throw new ArgumentException("Page must be greater than zero.");
        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100.");

        await GetPublishedMomentForInteractionAsync(momentId, userId, cancellationToken);
        var (comments, totalCount) = await _momentRepository.GetCommentsAsync(
            momentId, userId, page, pageSize, cancellationToken);

        return new GetMomentCommentsResult
        {
            MomentId = momentId,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Comments = comments.ToList()
        };
    }

    public async Task<MomentCommentResult> UpdateCommentAsync(
        Guid momentId, Guid commentId, Guid userId, string content,
        CancellationToken cancellationToken = default)
    {
        var (comment, _) = await GetCommentForActionAsync(momentId, commentId, userId, cancellationToken);
        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("You can only edit your own comments.");

        comment.UpdateContent(content);
        await _momentRepository.SaveCommentAsync(comment, cancellationToken);

        return await _momentRepository.GetCommentResultAsync(commentId, userId, cancellationToken)
            ?? throw new KeyNotFoundException("Comment was not found.");
    }

    public async Task DeleteCommentAsync(
        Guid momentId, Guid commentId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (comment, memoryOwnerId) = await GetCommentForActionAsync(
            momentId, commentId, userId, cancellationToken);

        if (comment.UserId != userId && memoryOwnerId != userId)
            throw new UnauthorizedAccessException("Only the comment author or Memory owner can delete this comment.");

        comment.Delete();
        await _momentRepository.SaveCommentAsync(comment, cancellationToken);
    }

    public Task<MomentCommentLikeResult> LikeCommentAsync(
        Guid momentId, Guid commentId, Guid userId,
        CancellationToken cancellationToken = default)
        => SetCommentLikeAsync(momentId, commentId, userId, true, cancellationToken);

    public Task<MomentCommentLikeResult> UnlikeCommentAsync(
        Guid momentId, Guid commentId, Guid userId,
        CancellationToken cancellationToken = default)
        => SetCommentLikeAsync(momentId, commentId, userId, false, cancellationToken);

    private async Task<MomentCommentLikeResult> SetCommentLikeAsync(
        Guid momentId, Guid commentId, Guid userId, bool isLiked,
        CancellationToken cancellationToken)
    {
        await GetCommentForActionAsync(momentId, commentId, userId, cancellationToken);
        await _momentRepository.SetCommentLikeAsync(commentId, userId, isLiked, cancellationToken);
        return await _momentRepository.GetCommentLikeSummaryAsync(commentId, userId, cancellationToken);
    }

    private async Task<MemoryMoment> GetPublishedMomentForInteractionAsync(
        Guid momentId, Guid userId, CancellationToken cancellationToken)
    {
        var moment = await _momentRepository.GetByIdAsync(momentId, cancellationToken);
        if (moment is null || moment.Status == MomentStatus.Removed)
            throw new KeyNotFoundException("Moment was not found.");
        if (moment.Status != MomentStatus.Published)
            throw new InvalidOperationException("Only published Moments can receive comments and comment likes.");

        var context = await _momentRepository.GetContributionContextAsync(
            moment.MemoryId, userId, cancellationToken);
        if (context is null) throw new KeyNotFoundException("Memory was not found.");

        EnsureCanViewMemory(context.Memory, context.Participant, userId);
        return moment;
    }

    private async Task<(MomentComment Comment, Guid MemoryOwnerId)> GetCommentForActionAsync(
        Guid momentId, Guid commentId, Guid userId, CancellationToken cancellationToken)
    {
        if (momentId == Guid.Empty || commentId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Valid Moment, comment and user IDs are required.");

        var comment = await _momentRepository.GetCommentByIdAsync(commentId, cancellationToken);
        if (comment is null || comment.MomentId != momentId)
            throw new KeyNotFoundException("Comment was not found.");

        await GetPublishedMomentForInteractionAsync(momentId, userId, cancellationToken);
        return (comment, comment.Moment.Memory.OwnerId);
    }

}
