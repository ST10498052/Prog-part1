using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Organisers manage events; both roles can view them.</summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
[SessionAuthorize]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public EventsController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
    }

    /// <summary>List all events, with optional filters.</summary>
    /// <remarks>Draft events are only visible to the Organiser who owns them.</remarks>
    /// <param name="location">Only events whose location contains this text.</param>
    /// <param name="eventType">Run, Walk or Cycle.</param>
    /// <param name="from">Only events on or after this date.</param>
    /// <response code="200">Returns the events ordered by date.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] string? location, [FromQuery] string? eventType, [FromQuery] DateTime? from)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var query = _db.Events.Include(e => e.Organiser).Include(e => e.Enrolments).AsQueryable();

        query = query.Where(e => e.Status != "Draft" || e.OrganiserId == userId);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.Location.Contains(location));
        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);
        if (from.HasValue)
            query = query.Where(e => e.EventDate >= from.Value);

        var list = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(list.Select(e => e.ToResponse(false)));
    }

    /// <summary>List the events I created (Organisers only), including enrolment counts.</summary>
    /// <response code="200">Returns my events.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can use this.</response>
    [HttpGet("mine")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMine()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var list = await _db.Events.Include(e => e.Organiser).Include(e => e.Enrolments)
            .Where(e => e.OrganiserId == userId)
            .OrderBy(e => e.EventDate)
            .ToListAsync();
        return Ok(list.Select(e => e.ToResponse(false)));
    }

    /// <summary>Get one event, including its categories.</summary>
    /// <response code="200">Returns the event.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found (or it is another Organiser's draft).</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Enrolments)
            .Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .SingleOrDefaultAsync(e => e.EventId == id);

        if (ev is null || (ev.Status == "Draft" && ev.OrganiserId != HttpContext.GetUserId()))
            return NotFound(new { message = "Event not found." });

        return Ok(ev.ToResponse(true));
    }

    /// <summary>Create a new event owned by the logged-in Organiser.</summary>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Organisers can create events.</response>
    [HttpPost]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] EventRequest request)
    {
        var userId = HttpContext.GetUserId()!.Value;

        var ev = new Event
        {
            OrganiserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description,
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm,
            EventType = request.EventType,
            Status = request.Status ?? "Published",
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ev.ToResponse(false));
    }

    /// <summary>Update an event. Only the Organiser who owns it may do this.</summary>
    /// <response code="200">Event updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).Include(e => e.Enrolments)
            .SingleOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != HttpContext.GetUserId()) return Forbid403("You can only edit your own events.");

        ev.Name = request.Name.Trim();
        ev.Description = request.Description;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = request.EventType;
        if (request.Status is not null) ev.Status = request.Status;

        await _db.SaveChangesAsync();
        return Ok(ev.ToResponse(false));
    }

    /// <summary>Upload (or replace) the banner image of one of my events (Organisers only).</summary>
    /// <remarks>
    /// Send the image as multipart/form-data in a field called "file". JPEG, PNG, GIF or WebP, up to 5 MB.
    /// The image is stored in Azure Blob Storage and the event's BannerImage is updated.
    /// </remarks>
    /// <response code="200">Banner saved. Returns the updated event.</response>
    /// <response code="400">Not a valid image, wrong type, or too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="503">Image storage is not available.</response>
    [HttpPost("{id:int}/banner")]
    [SessionAuthorize(RoleNames.Organiser)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _db.Events.Include(e => e.Organiser).Include(e => e.Enrolments)
            .SingleOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != HttpContext.GetUserId()) return Forbid403("You can only change the banner of your own events.");

        var (error, extension) = await ImageRules.ValidateAsync(file);
        if (error is not null) return BadRequest(new { message = error });

        var blobName = $"banners/{id}-{Guid.NewGuid():N}{extension}";
        try
        {
            await using var stream = file.OpenReadStream();
            await _blobs.UploadAsync(blobName, stream, file.ContentType);
        }
        catch (BlobStorageUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }

        var oldBanner = ev.BannerImage;
        ev.BannerImage = blobName;
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldBanner)) await _blobs.DeleteAsync(oldBanner);
        return Ok(ev.ToResponse(false));
    }

    /// <summary>Delete an event (and its categories). Only the owning Organiser may do this.</summary>
    /// <remarks>An event that already has enrolments cannot be deleted; cancel it instead by setting its Status to Cancelled.</remarks>
    /// <response code="204">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    /// <response code="409">The event has enrolments and cannot be deleted.</response>
    [HttpDelete("{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.Include(e => e.Categories).SingleOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != HttpContext.GetUserId()) return Forbid403("You can only delete your own events.");

        if (await _db.Enrolments.AnyAsync(en => en.EventId == id))
            return Conflict(new { message = "This event has enrolments and cannot be deleted. Set its status to Cancelled instead." });

        var banner = ev.BannerImage;
        _db.Categories.RemoveRange(ev.Categories);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(banner)) await _blobs.DeleteAsync(banner);
        return NoContent();
    }

    private ObjectResult Forbid403(string message) =>
        StatusCode(StatusCodes.Status403Forbidden, new { message });
}
