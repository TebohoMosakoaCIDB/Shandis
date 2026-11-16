
using Shandis.Domain.Common;
using Shandis.Domain.Enums;

namespace Shandis.Domain.Entities;

public class MomentMedia : Entity
{
    public Guid MomentId { get; private set; }

    public Guid UploadedByUserId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string BlobName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long FileSizeBytes { get; private set; }

    public MediaType MediaType { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime UploadedAtUtc { get; private set; }

    public MemoryMoment Moment { get; private set; } = null!;

    private MomentMedia()
    {
    }

    public MomentMedia(
    Guid momentId,
    Guid uploadedByUserId,
    string fileName,
    string blobName,
    string contentType,
    long fileSizeBytes,
    MediaType mediaType,
    int sortOrder = 0)
    {
        if (momentId == Guid.Empty)
            throw new ArgumentException("Moment is required.");

        if (uploadedByUserId == Guid.Empty)
            throw new ArgumentException("Uploader is required.");

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.");

        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name is required.");

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.");

        if (fileSizeBytes <= 0)
            throw new ArgumentException(
                "File size must be greater than zero.");

        if (!Enum.IsDefined(mediaType))
            throw new ArgumentException("Invalid media type.");

        if (sortOrder < 0)
            throw new ArgumentOutOfRangeException(nameof(sortOrder));

        MomentId = momentId;
        UploadedByUserId = uploadedByUserId;
        FileName = fileName.Trim();
        BlobName = blobName;
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        MediaType = mediaType;
        SortOrder = sortOrder;
        UploadedAtUtc = DateTime.UtcNow;
    }
}