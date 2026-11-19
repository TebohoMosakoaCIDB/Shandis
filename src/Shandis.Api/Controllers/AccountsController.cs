using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shandis.Api.Contracts.Accounts;
using Shandis.Infrastructure.Identity;

namespace Shandis.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    private static readonly HashSet<string> ReservedUserNames =
    [
        "admin", "api", "help", "login", "logout", "members", "memories",
        "notifications", "privacy", "profile", "register", "search",
        "settings", "shared", "shandis", "support", "terms", "u"
    ];

    private readonly UserManager<ShandisUser> _userManager;

    public AccountsController(UserManager<ShandisUser> userManager)
    {
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterAccountRequest request)
    {
        var userName = request.UserName.Trim().ToLowerInvariant();

        if (ReservedUserNames.Contains(userName))
            return BadRequest(new { message = "That username is reserved. Please choose another one." });

        var email = request.Email.Trim();

        var user = new ShandisUser
        {
            UserName = userName,
            Email = email,
            FirstName = userName,
            LastName = string.Empty,
            DisplayName = userName,
            EmailConfirmed = false,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new
            {
                errors = result.Errors.Select(x => x.Description)
            });
        }

        return Ok(new
        {
            message = "Your Shandis account has been created.",
            user.Id,
            user.UserName,
            user.Email
        });
    }
}