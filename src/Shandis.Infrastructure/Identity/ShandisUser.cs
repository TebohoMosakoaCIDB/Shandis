using Microsoft.AspNetCore.Identity;

namespace Shandis.Infrastructure.Identity;

public class ShandisUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public string? Bio { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
