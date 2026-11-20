using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shandis.Application.Features.Memories;

namespace Shandis.Api.Controllers;

[ApiController]
[Route("api/saved-memories")]
[Authorize]
public sealed class SavedMemoriesController(ISavedMemoryService service) : ControllerBase
{
    private Guid? CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<ActionResult<SavedMemoriesResult>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is not Guid userId) return Unauthorized();
        try { return Ok(await service.GetSavedAsync(userId, page, pageSize, cancellationToken)); }
        catch (ArgumentOutOfRangeException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{memoryId:guid}/status")]
    public async Task<ActionResult<object>> GetStatus(Guid memoryId, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not Guid userId) return Unauthorized();
        return Ok(new { isSaved = await service.IsSavedAsync(memoryId, userId, cancellationToken) });
    }

    [HttpPost("{memoryId:guid}")]
    public async Task<IActionResult> Save(Guid memoryId, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not Guid userId) return Unauthorized();
        var saved = await service.SaveAsync(memoryId, userId, cancellationToken);
        return saved ? NoContent() : NotFound();
    }

    [HttpDelete("{memoryId:guid}")]
    public async Task<IActionResult> Unsave(Guid memoryId, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not Guid userId) return Unauthorized();
        await service.UnsaveAsync(memoryId, userId, cancellationToken);
        return NoContent();
    }
}
