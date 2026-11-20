using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shandis.Application.Features.Follows;

namespace Shandis.Api.Controllers;

[ApiController]
[Route("api/follows")]
[Authorize]
public sealed class FollowsController(IFollowService followService) : ControllerBase
{
    [HttpPost("{userId:guid}")]
    [ProducesResponseType(typeof(FollowRelationshipResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Follow(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await followService.FollowAsync(
                currentUserId,
                userId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(typeof(FollowRelationshipResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Unfollow(
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentUserId))
            return Unauthorized();

        try
        {
            var result = await followService.UnfollowAsync(
                currentUserId,
                userId,
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                    User.FindFirstValue("sub");

        return Guid.TryParse(value, out userId);
    }
}
