using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shandis.Api.Contracts.Moments;
using Shandis.Application.Features.Moments;
using Shandis.Application.Features.Moments.Commands;
using Shandis.Application.Features.Moments.Results;

namespace Shandis.Api.Controllers;

[ApiController]
[Route("api/moments")]
[Authorize]
public class MomentsController : ControllerBase
{
    private readonly IMomentService _momentService;

    public MomentsController(IMomentService momentService)
    {
        _momentService = momentService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateMomentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromForm] CreateMomentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var streams = new List<Stream>();

        try
        {
            var mediaUploads = new List<MomentMediaUpload>();

            foreach (var file in request.Media)
            {
                if (file.Length <= 0)
                    return BadRequest($"The file '{file.FileName}' is empty.");

                var stream = file.OpenReadStream();
                streams.Add(stream);

                mediaUploads.Add(new MomentMediaUpload
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    FileStream = stream
                });
            }

            var command = new CreateMomentCommand
            {
                MemoryId = request.MemoryId,
                CreatedByUserId = userId,
                MomentType = request.MomentType,
                Title = request.Title,
                Content = request.Content,
                HappenedAtUtc = request.HappenedAtUtc,
                Media = mediaUploads
            };

            var result = await _momentService.CreateAsync(
                command,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }
    }

    [HttpGet("{momentId:guid}")]
    [ProducesResponseType(typeof(CreateMomentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid momentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _momentService.GetByIdAsync(
                momentId,
                userId,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        return Guid.TryParse(userIdValue, out userId);
    }

    [HttpGet("{memoryId:guid}/moments")]
    [ProducesResponseType(typeof(GetMomentsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMoments(
    Guid memoryId,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var userIdValue =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var result = await _momentService.GetByMemoryAsync(
                memoryId,
                userId,
                page,
                pageSize,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{momentId:guid}/approve")]
    [ProducesResponseType(typeof(CreateMomentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(
    Guid momentId,
    CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _momentService.ApproveAsync(
                momentId,
                userId,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{momentId:guid}/reject")]
    [ProducesResponseType(typeof(CreateMomentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(
        Guid momentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        try
        {
            var result = await _momentService.RejectAsync(
                momentId,
                userId,
                cancellationToken);

            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}