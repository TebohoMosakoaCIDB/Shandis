using Shandis.Domain.Common;

namespace Shandis.Domain.Entities;

public sealed class MemoryMedia : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSizeBytes { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }

    public Memory Memory { get; private set; } = null!;

    private MemoryMedia() { }

    public MemoryMedia(
        Guid memoryId,
        Guid uploadedByUserId,
        string fileName,
        string blobName,
        string contentType,
        long fileSizeBytes)
    {
        if (memoryId == Guid.Empty)
            throw new ArgumentException("Memory is required.", nameof(memoryId));
        if (uploadedByUserId == Guid.Empty)
            throw new ArgumentException("Uploader is required.", nameof(uploadedByUserId));
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Storage object path is required.", nameof(blobName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required.", nameof(contentType));
        if (fileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes));

        MemoryId = memoryId;
        UploadedByUserId = uploadedByUserId;
        FileName = Path.GetFileName(fileName.Trim());
        BlobName = blobName;
        ContentType = contentType.Trim();
        FileSizeBytes = fileSizeBytes;
        UploadedAtUtc = DateTime.UtcNow;
    }
}
