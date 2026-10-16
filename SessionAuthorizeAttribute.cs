using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Registration, login and logout. Login stores the user's id and role in the session.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly PasswordService _passwords;

    public AuthController(RaceDayDbContext db, PasswordService passwords)
    {
        _db = db;
        _passwords = passwords;
    }

    /// <summary>Register a new account as an Organiser or a Participant.</summary>
    /// <remarks>The password is hashed before it is stored. Emails must be unique.</remarks>
    /// <response code="201">Account created. Returns the new profile.</response>
    /// <response code="400">Validation failed (e.g. invalid role, short password).</response>
    /// <response code="409">An account with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (request.Role != RoleNames.Organiser && request.Role != RoleNames.Participant)
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "An account with this email already exists." });

        var role = await _db.Roles.SingleAsync(r => r.RoleName == request.Role);

        var user = new User
        {
            RoleId = role.RoleId,
            Role = role,
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = _passwords.Hash(request.Password),
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, user.ToResponse());
    }

    /// <summary>Log in with email and password and start a session.</summary>
    /// <remarks>A session cookie is issued. It carries the user's id and role on every later request.</remarks>
    /// <response code="200">Logged in. Returns the profile including the role.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Role).SingleOrDefaultAsync(u => u.Email == email);

        if (user is null || !_passwords.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        await HttpContext.Session.LoadAsync();
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(UserSessionExtensions.UserIdKey, user.UserId);
        HttpContext.Session.SetString(UserSessionExtensions.RoleKey, user.Role!.RoleName);

        return Ok(user.ToResponse());
    }

    /// <summary>Log out and end the current session.</summary>
    /// <response code="200">Logged out.</response>
    /// <response code="401">Not logged in.</response>
    [HttpPost("logout")]
    [SessionAuthorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}
