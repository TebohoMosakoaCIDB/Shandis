using System.ComponentModel.DataAnnotations;

namespace Shandis.Api.Contracts.Profiles;

public class UpdateProfileRequest
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Usernames may contain only letters, numbers and underscores.")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Bio { get; set; }
}