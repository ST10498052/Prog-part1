using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Organisers capture finish times and positions; everyone can see an event's leaderboard.</summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
[SessionAuthorize]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Capture the finish time and position for an enrolment (Organisers only).</summary>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Validation failed (a Finished result needs a finish time and position).</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment.</response>
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Capture(int enrolmentId, [FromBody] ResultRequest request)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).SingleOrDefaultAsync(e => e.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event!.OrganiserId != HttpContext.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only capture results for your own events." });

        var status = request.Status ?? "Finished";
        var problem = ValidateFinished(status, request);
        if (problem is not null) return BadRequest(new { message = problem });

        if (await _db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment. Use PUT /api/results/{id} to correct it." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, await LoadResponseAsync(result.ResultId));
    }

    /// <summary>Correct an existing result (Organisers only, for their own events).</summary>
    /// <response code="200">Result updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or not the owner of this event.</response>
    /// <response code="404">Result not found.</response>
    [HttpPut("results/{id:int}")]
    [SessionAuthorize(RoleNames.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] ResultRequest request)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .SingleOrDefaultAsync(r => r.ResultId == id);
        if (result is null) return NotFound(new { message = "Result not found." });
        if (result.Enrolment!.Event!.OrganiserId != HttpContext.GetUserId())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You can only edit results for your own events." });

        var status = request.Status ?? "Finished";
        var problem = ValidateFinished(status, request);
        if (problem is not null) return BadRequest(new { message = problem });

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = status;
        await _db.SaveChangesAsync();

        return Ok(await LoadResponseAsync(result.ResultId));
    }

    /// <summary>View the results (leaderboard) for an event, ordered by position.</summary>
    /// <response code="200">Returns the results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
            return NotFound(new { message = "Event not found." });

        var list = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Where(r => r.Enrolment!.EventId == eventId)
            .ToListAsync();

        var ordered = list
            .OrderBy(r => r.Position.HasValue ? 0 : 1)
            .ThenBy(r => r.Position)
            .Select(r => r.ToResponse());
        return Ok(ordered);
    }

    private static string? ValidateFinished(string status, ResultRequest request)
    {
        if (status == "Finished" && (request.FinishTime is null || request.Position is null))
            return "A Finished result needs both a finish time and a position.";
        return null;
    }

    private async Task<ResultResponse> LoadResponseAsync(int resultId)
    {
        var result = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .SingleAsync(r => r.ResultId == resultId);
        return result.ToResponse();
    }
}
