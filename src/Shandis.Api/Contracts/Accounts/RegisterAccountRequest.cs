using System.ComponentModel.DataAnnotations;

namespace Shandis.Api.Contracts.Accounts;

public sealed class RegisterAccountRequest
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Usernames may contain only letters, numbers and underscores.")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
}