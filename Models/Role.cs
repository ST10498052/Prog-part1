namespace RaceDay.Api.Models;

/// <summary>Lookup table for the two system roles.</summary>
public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public ICollection<User> Users { get; set; } = new List<User>();
}

/// <summary>Role names used throughout the API.</summary>
public static class RoleNames
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}
