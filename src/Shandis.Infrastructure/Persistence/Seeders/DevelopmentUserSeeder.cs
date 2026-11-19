using Microsoft.AspNetCore.Identity;
using Shandis.Infrastructure.Identity;

namespace Shandis.Infrastructure.Persistence.Seeders;

public static class DevelopmentUserSeeder
{
    private sealed record TestUser(
        string FirstName,
        string LastName,
        string DisplayName,
        string Email);

    private static readonly TestUser[] TestUsers =
    [
        new("Olivia", "Owner", "Olivia Owner", "owner@shandis.local"),
        new("Sam", "Contributor", "Sam Contributor", "contributor@shandis.local"),
        new("Taylor", "Viewer", "Taylor Viewer", "viewer@shandis.local")
    ];

    public static async Task SeedAsync(
        UserManager<ShandisUser> userManager,
        string password)
    {
        ArgumentNullException.ThrowIfNull(userManager);

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("A development test password is required.", nameof(password));

        foreach (var testUser in TestUsers)
        {
            var existingUser = await userManager.FindByEmailAsync(testUser.Email);

            if (existingUser is not null)
                continue;

            var user = new ShandisUser
            {
                UserName = testUser.Email,
                Email = testUser.Email,
                EmailConfirmed = true,
                FirstName = testUser.FirstName,
                LastName = testUser.LastName,
                DisplayName = testUser.DisplayName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"Could not seed development user '{testUser.Email}': {errors}");
            }
        }
    }
}
