
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shandis.Api.Contracts.Invitations;
using Shandis.Application.Features.Invitations;

namespace Shandis.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly IInvitationService _invitationService;

    public InvitationsController(IInvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpPost("memories/{memoryId:guid}/invitations")]
    public async Task<ActionResult> Create(
        Guid memoryId,
        [FromBody] CreateInvitationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _invitationService.CreateAsync(
                new CreateInvitationCommand
                {
                    MemoryId = memoryId,
                    InvitedByUserId = userId.Value,
                    Invitee = request.Invitee,
                    AssignedRole = request.AssignedRole
                },
                cancellationToken);

            return Created(
                $"/api/memories/{memoryId}/invitations/{result.Id}",
                new
                {
                    result.Id,
                    result.MemoryId,
                    result.InvitedEmail,
                    result.AssignedRole,
                    result.Status,
                    result.ExpiresAtUtc,
                    result.CreatedAtUtc
                });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("memories/{memoryId:guid}/invitations")]
    public async Task<ActionResult<IReadOnlyList<InvitationItemResponse>>> GetForMemory(
        Guid memoryId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        try
        {
            var invitations = await _invitationService.GetForMemoryAsync(
                memoryId,
                userId.Value,
                cancellationToken);

            var response = invitations.Select(MapInvitation).ToList();

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { message = exception.Message });
        }
    }

    [HttpGet("invitations/received")]
    public async Task<ActionResult<ReceivedInvitationsResponse>> GetReceived(
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        try
        {
            var result = await _invitationService.GetReceivedAsync(
                userId.Value,
                cancellationToken);

            return Ok(new ReceivedInvitationsResponse
            {
                Items = result.Items.Select(MapInvitation).ToList()
            });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    private Guid? GetCurrentUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userIdValue, out var userId)
            ? userId
            : null;
    }

    private static InvitationItemResponse MapInvitation(
        InvitationItemResult invitation)
    {
        return new InvitationItemResponse
        {
            Id = invitation.Id,
            MemoryId = invitation.MemoryId,
            MemoryTitle = invitation.MemoryTitle,
            InvitedByUserId = invitation.InvitedByUserId,
            InvitedUserId = invitation.InvitedUserId,
            InvitedEmail = invitation.InvitedEmail,
            AssignedRole = invitation.AssignedRole,
            Status = invitation.Status,
            ExpiresAtUtc = invitation.ExpiresAtUtc,
            CreatedAtUtc = invitation.CreatedAtUtc
        };
    }

    [HttpPost("invitations/{invitationId:guid}/accept")]
    public async Task<IActionResult> Accept(
    Guid invitationId,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Your account email address is unavailable." });

        try
        {
            await _invitationService.AcceptAsync(
                invitationId,
                userId.Value,
                email,
                cancellationToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("invitations/{invitationId:guid}/decline")]
    public async Task<IActionResult> Decline(
    Guid invitationId,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        var email = User.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Your account email address is unavailable." });

        try
        {
            await _invitationService.DeclineAsync(
                invitationId,
                userId.Value,
                email,
                cancellationToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpDelete("invitations/{invitationId:guid}")]
    public async Task<IActionResult> Revoke(
    Guid invitationId,
    CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
            return Unauthorized();

        try
        {
            await _invitationService.RevokeAsync(
                invitationId,
                userId.Value,
                cancellationToken);

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }


}
