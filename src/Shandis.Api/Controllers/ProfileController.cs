using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shandis.Api.Contracts.Profiles;
using Shandis.Infrastructure.Identity;

namespace Shandis.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private static readonly HashSet<string> ReservedUserNames =
    [
        "admin", "api", "help", "login", "logout", "memories",
        "notifications", "privacy", "profile", "register", "search",
        "settings", "shared", "shandis", "support", "terms"
    ];

    private readonly UserManager<ShandisUser> _userManager;

    public ProfileController(UserManager<ShandisUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        return Ok(MapProfile(user));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Unauthorized();

        var userName = request.UserName.Trim().ToLowerInvariant();

        if (ReservedUserNames.Contains(userName))
            return BadRequest(new { message = "That username is reserved. Please choose another one." });

        var userNameResult = await _userManager.SetUserNameAsync(user, userName);

        if (!userNameResult.Succeeded)
        {
            return BadRequest(new
            {
                errors = userNameResult.Errors.Select(x => x.Description)
            });
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.DisplayName = request.DisplayName.Trim();
        user.Bio = string.IsNullOrWhiteSpace(request.Bio)
            ? null
            : request.Bio.Trim();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(x => x.Description)
            });
        }

        return Ok(MapProfile(user));
    }

    private static UserProfileResponse MapProfile(ShandisUser user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Bio = user.Bio,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }
}