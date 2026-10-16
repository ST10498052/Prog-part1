namespace RaceDay.Api.Models;

/// <summary>Finish time and position for one enrolment (one-to-one).</summary>
public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    /// <summary>Finished, DNF or DSQ.</summary>
    public string Status { get; set; } = "Finished";
}
