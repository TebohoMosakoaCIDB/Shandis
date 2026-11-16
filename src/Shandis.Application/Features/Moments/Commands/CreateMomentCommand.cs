using Shandis.Domain.Enums;

namespace Shandis.Application.Features.Moments.Commands;

public class CreateMomentCommand
{
    public Guid MemoryId { get; set; }

    public Guid CreatedByUserId { get; set; }

    public MomentType MomentType { get; set; }

    public string? Title { get; set; }

    public string? Content { get; set; }

    public DateTime? HappenedAtUtc { get; set; }

    public List<MomentMediaUpload> Media { get; set; } = [];
}

public class MomentMediaUpload
{
    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public Stream FileStream { get; set; } = Stream.Null;
}
