using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Shandis.Domain.Enums;

namespace Shandis.Api.Contracts.Moments;

public class CreateMomentRequest
{
    [Required]
    public Guid MemoryId { get; set; }

    [Required]
    [EnumDataType(typeof(MomentType))]
    public MomentType MomentType { get; set; }

    [MaxLength(200)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    public DateTime? HappenedAtUtc { get; set; }

    public List<IFormFile> Media { get; set; } = [];
}