using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CategoryRequest
{
    /// <example>Under 20</example>
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Range(0, 120)]
    public int? MaxAge { get; set; }

    [Range(0.01, 999.99)]
    public decimal? DistanceKm { get; set; }

    /// <example>150.00</example>
    [Range(0, 100000)]
    public decimal EntryFee { get; set; }

    /// <example>500</example>
    [Range(1, 1000000)]
    public int MaxParticipants { get; set; }
}

public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
}
