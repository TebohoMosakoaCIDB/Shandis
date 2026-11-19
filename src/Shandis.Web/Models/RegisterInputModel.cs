using System.ComponentModel.DataAnnotations;

namespace Shandis.Web.Models;

public sealed class RegisterInputModel
{
    [Required]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Your username must be between 3 and 30 characters.")]
    [RegularExpression("^[a-zA-Z0-9_]+$", ErrorMessage = "Use only letters, numbers and underscores.")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8, ErrorMessage = "Your password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}