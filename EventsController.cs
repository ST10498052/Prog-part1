using System.Text;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant home: browse upcoming events, see details and enrol.</summary>
[AppAuthorize(AppRoles.Participant)]
public class EventsController : Controller
{
    private readonly ApiClient _api;

    public EventsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(string? location, string? eventType, DateTime? from)
    {
        var fromDate = from ?? DateTime.Today;

        var path = new StringBuilder("api/events?from=" + fromDate.ToString("yyyy-MM-dd"));
        if (!string.IsNullOrWhiteSpace(location))
            path.Append("&location=").Append(Uri.EscapeDataString(location.Trim()));
        if (!string.IsNullOrWhiteSpace(eventType))
            path.Append("&eventType=").Append(Uri.EscapeDataString(eventType));

        var result = await _api.GetAsync<List<EventDto>>(path.ToString());

        var model = new EventListViewModel
        {
            Events = result.Data ?? new List<EventDto>(),
            Location = location,
            EventType = eventType,
            From = fromDate,
            Error = result.Success ? null : result.Message
        };
        return View(model);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!ev.Success || ev.Data is null)
        {
            TempData["Error"] = string.IsNullOrEmpty(ev.Message) ? "That event could not be found." : ev.Message;
            return RedirectToAction(nameof(Index));
        }

        var mine = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
        var enrolment = mine.Data?.FirstOrDefault(e => e.EventId == id);

        return View(new EventDetailsViewModel { Event = ev.Data, Enrolment = enrolment });
    }

    [HttpPost]
    public async Task<IActionResult> Enrol(int id, int? categoryId)
    {
        if (categoryId is null)
        {
            TempData["Error"] = "Please choose a category before you enrol.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _api.PostAsync<EnrolmentDto>($"api/events/{id}/enrolments", new { categoryId });
        if (result.Success)
            TempData["Success"] = $"You are enrolled! Your bib number is {result.Data?.BibNumber}.";
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Details), new { id });
    }
}
