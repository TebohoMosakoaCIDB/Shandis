
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shandis.Api.Contracts.Memories;
using Shandis.Application.Features.Memories;
using Shandis.Domain.Enums;

namespace Shandis.Api.Controllers;

[ApiController]
[Route("api/memories")]
[Authorize]
public class MemoriesController : ControllerBase
{
    private readonly IMemoryService _memoryService;

    public MemoriesController(IMemoryService memoryService)
    {
        _memoryService = memoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(MemoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MemoryResponse>> Create(
        [FromBody] CreateMemoryRequest request,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var ownerId))
            return Unauthorized();

        if (!Enum.TryParse<MemoryVisibility>(
                request.Visibility, true, out var visibility))
        {
            return BadRequest("Invalid Memory visibility.");
        }

        if (!Enum.TryParse<MemoryContributionPolicy>(
                request.ContributionPolicy, true, out var contributionPolicy))
        {
            return BadRequest("Invalid Memory contribution policy.");
        }

        var command = new CreateMemoryCommand
        {
            OwnerId = ownerId,
            Title = request.Title,
            Description = request.Description,
            CategoryId = request.CategoryId,
            PlaceId = request.PlaceId,
            Visibility = visibility,
            ContributionPolicy = contributionPolicy,
            StartDateUtc = request.StartDate?.UtcDateTime,
            EndDateUtc = request.EndDate?.UtcDateTime,
            TimeZoneId = request.TimeZoneId
        };

        try
        {
            var result = await _memoryService.CreateAsync(
                command,
                cancellationToken);

            var response = new MemoryResponse
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                OwnerId = result.OwnerId,
                CategoryId = result.CategoryId,
                PlaceId = result.PlaceId,
                Visibility = result.Visibility,
                ContributionPolicy = result.ContributionPolicy,
                Status = result.Status,
                CreatedAtUtc = result.CreatedAtUtc
            };

            return Created(
                $"/api/memories/{response.Id}",
                response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Publish(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var published = await _memoryService.PublishAsync(
                id,
                userId,
                cancellationToken);

            if (!published)
                return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MemoryDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MemoryDetailsResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var result = await _memoryService.GetByIdAsync(
            id,
            userId,
            cancellationToken);

        if (result is null)
            return NotFound();

        var response = new MemoryDetailsResponse
        {
            Id = result.Id,
            Title = result.Title,
            Description = result.Description,
            OwnerId = result.OwnerId,
            CategoryId = result.CategoryId,
            CategoryName = result.CategoryName,
            PlaceId = result.PlaceId,
            PlaceName = result.PlaceName,
            PlaceAddress = result.PlaceAddress,
            Visibility = result.Visibility,
            ContributionPolicy = result.ContributionPolicy,
            Status = result.Status,
            StartDateUtc = result.StartDateUtc,
            EndDateUtc = result.EndDateUtc,
            TimeZoneId = result.TimeZoneId,
            CoverMediaId = result.CoverMediaId,
            CreatedAtUtc = result.CreatedAtUtc,
            PublishedAtUtc = result.PublishedAtUtc
        };

        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(MemoryDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MemoryDetailsResponse>> Update(
    Guid id,
    UpdateMemoryRequest request,
    CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var command = new UpdateMemoryCommand
            {
                MemoryId = id,
                RequestingUserId = userId,
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId,
                PlaceId = request.PlaceId,
                StartDateUtc = request.StartDate?.UtcDateTime,
                EndDateUtc = request.EndDate?.UtcDateTime,
                TimeZoneId = request.TimeZoneId
            };

            var result = await _memoryService.UpdateAsync(
                command,
                cancellationToken);

            if (result is null)
                return NotFound();

            var response = new MemoryDetailsResponse
            {
                Id = result.Id,
                Title = result.Title,
                Description = result.Description,
                OwnerId = result.OwnerId,
                CategoryId = result.CategoryId,
                CategoryName = result.CategoryName,
                PlaceId = result.PlaceId,
                PlaceName = result.PlaceName,
                PlaceAddress = result.PlaceAddress,
                Visibility = result.Visibility,
                ContributionPolicy = result.ContributionPolicy,
                Status = result.Status,
                StartDateUtc = result.StartDateUtc,
                EndDateUtc = result.EndDateUtc,
                TimeZoneId = result.TimeZoneId,
                CoverMediaId = result.CoverMediaId,
                CreatedAtUtc = result.CreatedAtUtc,
                PublishedAtUtc = result.PublishedAtUtc
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(
    Guid id,
    CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var archived = await _memoryService.ArchiveAsync(
                id,
                userId,
                cancellationToken);

            if (!archived)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var deleted = await _memoryService.DeleteAsync(
            id,
            userId,
            cancellationToken);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(MyMemoriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MyMemoriesResponse>> GetMyMemories(
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var result = await _memoryService.GetMyMemoriesAsync(
                userId,
                page,
                pageSize,
                cancellationToken);

            var response = new MyMemoriesResponse
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,

                Items = result.Items.Select(memory => new MyMemoryItemResponse
                {
                    Id = memory.Id,
                    Title = memory.Title,
                    Description = memory.Description,
                    OwnerId = memory.OwnerId,
                    IsOwner = memory.IsOwner,
                    CategoryId = memory.CategoryId,
                    CategoryName = memory.CategoryName,
                    PlaceId = memory.PlaceId,
                    PlaceName = memory.PlaceName,
                    Visibility = memory.Visibility,
                    ContributionPolicy = memory.ContributionPolicy,
                    Status = memory.Status,
                    StartDateUtc = memory.StartDateUtc,
                    EndDateUtc = memory.EndDateUtc,
                    TimeZoneId = memory.TimeZoneId,
                    CoverMediaId = memory.CoverMediaId,
                    CreatedAtUtc = memory.CreatedAtUtc,
                    PublishedAtUtc = memory.PublishedAtUtc
                }).ToList()
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


    [HttpGet("shared-with-me")]
    [ProducesResponseType(typeof(SharedMemoriesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SharedMemoriesResponse>> GetSharedWithMe(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdValue, out var userId))
            return Unauthorized();

        try
        {
            var result = await _memoryService.GetSharedWithMeAsync(
                userId,
                page,
                pageSize,
                cancellationToken);

            var response = new SharedMemoriesResponse
            {
                Page = result.Page,
                PageSize = result.PageSize,
                TotalCount = result.TotalCount,
                TotalPages = result.TotalPages,

                Items = result.Items.Select(memory => new SharedMemoryItemResponse
                {
                    Id = memory.Id,
                    Title = memory.Title,
                    Description = memory.Description,
                    OwnerId = memory.OwnerId,
                    CategoryId = memory.CategoryId,
                    CategoryName = memory.CategoryName,
                    PlaceId = memory.PlaceId,
                    PlaceName = memory.PlaceName,
                    Visibility = memory.Visibility,
                    ContributionPolicy = memory.ContributionPolicy,
                    Status = memory.Status,
                    MyRole = memory.MyRole,
                    StartDateUtc = memory.StartDateUtc,
                    EndDateUtc = memory.EndDateUtc,
                    TimeZoneId = memory.TimeZoneId,
                    CoverMediaId = memory.CoverMediaId,
                    CreatedAtUtc = memory.CreatedAtUtc,
                    PublishedAtUtc = memory.PublishedAtUtc
                }).ToList()
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }


}
