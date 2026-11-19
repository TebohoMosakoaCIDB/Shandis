using System.ComponentModel.DataAnnotations;

namespace Shandis.Web.Models;

public sealed class UpdateProfileRequest
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Use letters, numbers and underscores only.")]
    public string UserName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }
}

public sealed class MemberSearchItem
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}
