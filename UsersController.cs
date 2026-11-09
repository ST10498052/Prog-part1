using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>The logged-in user's own profile, enrolment history and results.</summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[SessionAuthorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordService _passwords;
    private readonly IBlobStorageService _blobs;

    public UsersController(RaceDayDbContext db, PasswordService passwords, IBlobStorageService blobs)
    {
        _db = db;
        _passwords = passwords;
        _blobs = blobs;
    }

    /// <summary>View my own profile.</summary>
    /// <response code="200">Returns the logged-in user's profile.</response>
    /// <response code="401">Not logged in.</response>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == userId);
        return user is null ? Unauthorized() : Ok(user.ToResponse());
    }

    /// <summary>Update my own profile (name and phone number).</summary>
    /// <response code="200">Returns the updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return Unauthorized();

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        await _db.SaveChangesAsync();

        return Ok(user.ToResponse());
    }

    /// <summary>Upload or replace my profile picture.</summary>
    /// <remarks>
    /// Send the image as multipart/form-data in a field called "file". JPEG, PNG, GIF or WebP, up to 5 MB.
    /// The image is stored in Azure Blob Storage.
    /// </remarks>
    /// <response code="200">Picture saved. Returns the updated profile.</response>
    /// <response code="400">Not a valid image, wrong type, or too large.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="503">Image storage is not available.</response>
    [HttpPost("me/profile-picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return Unauthorized();

        var (error, extension) = await ImageRules.ValidateAsync(file);
        if (error is not null) return BadRequest(new { message = error });

        var blobName = $"profiles/{userId}-{Guid.NewGuid():N}{extension}";
        try
        {
            await using var stream = file.OpenReadStream();
            await _blobs.UploadAsync(blobName, stream, file.ContentType);
        }
        catch (BlobStorageUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }

        var oldPicture = user.ProfilePicture;
        user.ProfilePicture = blobName;
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldPicture)) await _blobs.DeleteAsync(oldPicture);
        return Ok(user.ToResponse());
    }

    /// <summary>Change my password.</summary>
    /// <response code="200">Password changed.</response>
    /// <response code="400">Validation failed (new password too short).</response>
    /// <response code="401">Not logged in, or the current password is wrong.</response>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserId == userId);
        if (user is null) return Unauthorized();

        if (!_passwords.Verify(request.CurrentPassword, user.PasswordHash))
            return Unauthorized(new { message = "Current password is incorrect." });

        user.PasswordHash = _passwords.Hash(request.NewPassword);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Password updated." });
    }

    /// <summary>List every event and category I have entered (Participants only).</summary>
    /// <response code="200">Returns my enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can use this.</response>
    [HttpGet("me/enrolments")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyEnrolments()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var list = await _db.Enrolments
            .Include(e => e.Participant).Include(e => e.Event).Include(e => e.Category).Include(e => e.Result)
            .Where(e => e.ParticipantId == userId)
            .OrderBy(e => e.EnrolmentDate)
            .ToListAsync();
        return Ok(list.Select(e => e.ToResponse()));
    }

    /// <summary>View my own results across all events (Participants only).</summary>
    /// <response code="200">Returns my results.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Only Participants can use this.</response>
    [HttpGet("me/results")]
    [SessionAuthorize(RoleNames.Participant)]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MyResults()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var list = await _db.Results
            .Include(r => r.Enrolment).ThenInclude(e => e!.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e!.Category)
            .Where(r => r.Enrolment!.ParticipantId == userId)
            .ToListAsync();
        return Ok(await ResultResponseBuilder.BuildAsync(_db, list));
    }
}
