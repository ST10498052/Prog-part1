using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    /// <example>Lindiwe Nkosi</example>
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <example>lindiwe@example.com</example>
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    /// <summary>At least 8 characters.</summary>
    /// <example>Password123!</example>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Either "Organiser" or "Participant".</summary>
    /// <example>Participant</example>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <example>0611122233</example>
    [RegularExpression(@"^[0-9+ ()-]{7,20}$", ErrorMessage = "Phone number format is invalid.")]
    public string? PhoneNumber { get; set; }
}

public class LoginRequest
{
    /// <example>lindiwe@example.com</example>
    [Required]
    public string Email { get; set; } = string.Empty;

    /// <example>Password123!</example>
    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required, StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [RegularExpression(@"^[0-9+ ()-]{7,20}$", ErrorMessage = "Phone number format is invalid.")]
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;
}

public class UserProfileResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
