using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class EventRequest
{
    /// <example>Soweto Marathon 2027</example>
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <example>2027-11-07T06:00:00</example>
    [Required]
    public DateTime EventDate { get; set; }

    /// <example>FNB Stadium, Soweto</example>
    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <example>21.1</example>
    [Range(0.01, 999.99)]
    public decimal DistanceKm { get; set; }

    /// <summary>Run, Walk or Cycle.</summary>
    /// <example>Run</example>
    [Required, RegularExpression("^(Run|Walk|Cycle)$", ErrorMessage = "EventType must be Run, Walk or Cycle.")]
    public string EventType { get; set; } = string.Empty;

    /// <summary>Optional. Draft, Published, Cancelled or Completed. Defaults to Published on create.</summary>
    [RegularExpression("^(Draft|Published|Cancelled|Completed)$", ErrorMessage = "Status must be Draft, Published, Cancelled or Completed.")]
    public string? Status { get; set; }
}

public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<CategoryResponse>? Categories { get; set; }
}
