using System.ComponentModel.DataAnnotations;

namespace Shandis.Web.Models;

public sealed class LoginInputModel
{
    [Required]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
