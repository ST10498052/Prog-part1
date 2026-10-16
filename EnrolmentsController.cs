using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Participants enter events by choosing a category; Organisers view who has entered.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[SessionAuthorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Enter an event under a chosen category (Participants only).</summary>
    /// <remarks>Records the link between the participant, the event and the category, and assigns a unique bib number.</remarks>
    /// <response code="201">Enrolled. Returns the enrolment including the bib number.</response>
    /// <response code="400">Validation failed (no category supplied).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can enrol.</response>
    /// <response code="404">Event not found, or the category does not belong to this event.</response>
    /// <response code="409">Already enrolled, category full, or the event is closed.</response>
    [HttpPost("events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Enrol(int eventId, [FromBody] EnrolRequest request)
    {
        var participantId = HttpContext.GetUserId()!.Value;

        var ev = await _db.Events.SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });

        if (ev.Status == "Cancelled" || ev.Status == "Completed")
            return Conflict(new { message = $"This event is {ev.Status.ToLower()} and is not accepting enrolments." });

        var category = await _db.Categories
            .SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId);
        if (category is null)
            return NotFound(new { message = "That category does not exist for this event." });

        if (await _db.Enrolments.AnyAsync(en => en.ParticipantId == participantId && en.EventId == eventId))
            return Conflict(new { message = "You are already enrolled in this event." });

        var taken = await _db.Enrolments.CountAsync(en => en.CategoryId == category.CategoryId && en.Status == "Confirmed");
        if (taken >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            EventId = eventId,
            CategoryId = category.CategoryId,
            EnrolmentDate = DateTime.UtcNow,
            BibNumber = await NextBibNumberAsync(eventId),
            Status = "Confirmed"
        };

        _db.Enrolments.Add(enrolment);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique index (participant + event, or bib number) was hit by a simultaneous request.
            return Conflict(new { message = "Enrolment could not be completed. You may already be enrolled." });
        }

        var saved = await _db.Enrolments
            .Include(e => e.Participant).Include(e => e.Event).Include(e => e.Category)
            .SingleAsync(e => e.EnrolmentId == enrolment.EnrolmentId);

        return StatusCode(StatusCodes.Status201Created, saved.ToResponse());
    }

    /// <summary>List everyone enrolled in one of my events (Organisers only).</summary>
    /// <response code="200">Returns the enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/enrolments")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        var ev = await _db.Events.SingleOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != HttpContext.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only view enrolments for your own events." });

        var list = await _db.Enrolments
            .Include(e => e.Participant).Include(e => e.Event).Include(e => e.Category)
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();
        return Ok(list.Select(e => e.ToResponse()));
    }

    /// <summary>Withdraw my own enrolment (Participants only).</summary>
    /// <response code="204">Enrolment withdrawn.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not a Participant, or this is not your enrolment.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result has already been captured for this enrolment.</response>
    [HttpDelete("enrolments/{id:int}")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(int id)
    {
        var enrolment = await _db.Enrolments.SingleOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.ParticipantId != HttpContext.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only withdraw your own enrolment." });

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == id))
            return Conflict(new { message = "A result has already been captured, so this enrolment cannot be withdrawn." });

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Bib numbers look like "12-0001" (event id + running number) and must be unique and <= 10 chars.
    private async Task<string> NextBibNumberAsync(int eventId)
    {
        var next = await _db.Enrolments.CountAsync(e => e.EventId == eventId) + 1;
        string bib;
        do
        {
            bib = $"{eventId}-{next:D4}";
            next++;
        }
        while (await _db.Enrolments.AnyAsync(e => e.BibNumber == bib));
        return bib;
    }
}
