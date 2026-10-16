using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Maps database models to the response DTOs (so password hashes etc. are never exposed).</summary>
public static class Mappers
{
    public static UserProfileResponse ToResponse(this User u) => new()
    {
        UserId = u.UserId,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role?.RoleName ?? string.Empty,
        PhoneNumber = u.PhoneNumber,
        CreatedAt = u.CreatedAt
    };

    public static CategoryResponse ToResponse(this Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        Name = c.Name,
        Description = c.Description,
        MinAge = c.MinAge,
        MaxAge = c.MaxAge,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants
    };

    public static EventResponse ToResponse(this Event e, bool includeCategories) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        Name = e.Name,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        Status = e.Status,
        Categories = includeCategories ? e.Categories.Select(c => c.ToResponse()).ToList() : null
    };

    public static EnrolmentResponse ToResponse(this Enrolment en) => new()
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant?.FullName ?? string.Empty,
        EventId = en.EventId,
        EventName = en.Event?.Name ?? string.Empty,
        CategoryId = en.CategoryId,
        CategoryName = en.Category?.Name ?? string.Empty,
        BibNumber = en.BibNumber,
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status
    };

    public static ResultResponse ToResponse(this Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        EventId = r.Enrolment?.EventId ?? 0,
        EventName = r.Enrolment?.Event?.Name ?? string.Empty,
        ParticipantName = r.Enrolment?.Participant?.FullName ?? string.Empty,
        BibNumber = r.Enrolment?.BibNumber ?? string.Empty,
        CategoryName = r.Enrolment?.Category?.Name ?? string.Empty,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status
    };
}
