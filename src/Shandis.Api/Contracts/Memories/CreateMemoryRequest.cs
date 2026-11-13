
using System.ComponentModel.DataAnnotations;

namespace Shandis.Api.Contracts.Memories;

public class CreateMemoryRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid? PlaceId { get; set; }

    public string Visibility { get; set; } = "Private";

    public string ContributionPolicy { get; set; } = "InvitationOnly";

    public DateTimeOffset? StartDate { get; set; }

    public DateTimeOffset? EndDate { get; set; }

    [Required]
    [MaxLength(100)]
    public string TimeZoneId { get; set; } = "Africa/Johannesburg";
}
