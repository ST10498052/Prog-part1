namespace RaceDay.Api.Models;

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public Role? Role { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    /// <summary>Blob name of the profile picture in Azure Blob Storage (e.g. profiles/3-abc.png).</summary>
    public string? ProfilePicture { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Event> OrganisedEvents { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
