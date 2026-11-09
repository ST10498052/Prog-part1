using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Turns Result entities into responses and fills in TotalFinishers, so the UI can
/// show a position such as "87th out of 512 finishes".
/// The results passed in must have Enrolment, Event, Participant and Category loaded.
/// </summary>
public static class ResultResponseBuilder
{
    public static async Task<List<ResultResponse>> BuildAsync(RaceDayDbContext db, IEnumerable<Result> results)
    {
        var list = results.ToList();
        var eventIds = list.Select(r => r.Enrolment!.EventId).Distinct().ToList();

        var totals = await db.Results
            .Where(r => r.Status == "Finished" && eventIds.Contains(r.Enrolment!.EventId))
            .GroupBy(r => r.Enrolment!.EventId)
            .Select(g => new { EventId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EventId, x => x.Count);

        var responses = new List<ResultResponse>();
        foreach (var result in list)
        {
            var dto = result.ToResponse();
            dto.TotalFinishers = totals.TryGetValue(dto.EventId, out var count) ? count : 0;
            responses.Add(dto);
        }
        return responses;
    }
}
