namespace RaceDay.Api.Models;

/// <summary>Links a Participant to an Event and the Category they selected.</summary>
public class Enrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public User? Participant { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string BibNumber { get; set; } = string.Empty;
    /// <summary>Confirmed, Cancelled or Withdrawn.</summary>
    public string Status { get; set; } = "Confirmed";

    public Result? Result { get; set; }
}
