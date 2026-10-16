using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Age or distance categories for an event (e.g. Under 20, Senior, 10km, 21km).</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[SessionAuthorize]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>List the categories available for an event.</summary>
    /// <response code="200">Returns the categories.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var list = await _db.Categories.Where(c => c.EventId == eventId).OrderBy(c => c.CategoryId).ToListAsync();
        return Ok(list.Select(c => c.ToResponse()));
    }

    /// <summary>Add a category to one of my events (Organisers only).</summary>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed (e.g. MinAge greater than MaxAge).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("events/{eventId:int}/categories")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, [FromBody] CategoryRequest request)
    {
        var ev = await _db.Events.SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != HttpContext.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only add categories to your own events." });

        if (request.MinAge.HasValue && request.MaxAge.HasValue && request.MinAge > request.MaxAge)
            return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name.Trim(),
            Description = request.Description,
            MinAge = request.MinAge,
            MaxAge = request.MaxAge,
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, category.ToResponse());
    }

    /// <summary>Update a category (Organisers only, for their own events).</summary>
    /// <response code="200">Category updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    [HttpPut("categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound(new { message = "Category not found." });
        if (category.Event!.OrganiserId != HttpContext.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only edit categories of your own events." });

        if (request.MinAge.HasValue && request.MaxAge.HasValue && request.MinAge > request.MaxAge)
            return BadRequest(new { message = "MinAge cannot be greater than MaxAge." });

        category.Name = request.Name.Trim();
        category.Description = request.Description;
        category.MinAge = request.MinAge;
        category.MaxAge = request.MaxAge;
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;

        await _db.SaveChangesAsync();
        return Ok(category.ToResponse());
    }

    /// <summary>Delete a category that has no enrolments (Organisers only, for their own events).</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of the event.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category has enrolments and cannot be deleted.</response>
    [HttpDelete("categories/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound(new { message = "Category not found." });
        if (category.Event!.OrganiserId != HttpContext.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only delete categories of your own events." });

        if (await _db.Enrolments.AnyAsync(en => en.CategoryId == id))
            return Conflict(new { message = "This category has enrolments and cannot be deleted." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
