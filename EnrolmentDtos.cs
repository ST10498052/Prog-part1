using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class EnrolRequest
{
    /// <summary>The category (belonging to the event) the participant is entering.</summary>
    [Required]
    public int? CategoryId { get; set; }
}

public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string BibNumber { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    /// <summary>What the UI should show: Cancelled (event cancelled), Completed (result captured) or the enrolment status.</summary>
    public string DisplayStatus { get; set; } = string.Empty;
}
