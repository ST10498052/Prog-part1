using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class ResultRequest
{
    /// <summary>Finish time as hh:mm:ss. Required when Status is Finished.</summary>
    /// <example>01:52:30</example>
    public TimeSpan? FinishTime { get; set; }

    /// <summary>Finishing position. Required when Status is Finished.</summary>
    /// <example>15</example>
    [Range(1, 1000000)]
    public int? Position { get; set; }

    /// <summary>Finished, DNF or DSQ. Defaults to Finished.</summary>
    /// <example>Finished</example>
    [RegularExpression("^(Finished|DNF|DSQ)$", ErrorMessage = "Status must be Finished, DNF or DSQ.")]
    public string? Status { get; set; }
}

public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string BibNumber { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;
}
