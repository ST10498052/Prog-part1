using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organiser dashboard: events created, enrolments per event and upcoming dates.</summary>
[AppAuthorize(AppRoles.Organiser)]
public class DashboardController : Controller
{
    private readonly ApiClient _api;

    public DashboardController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (!result.Success) ViewBag.Error = result.Message;

        var events = (result.Data ?? new List<EventDto>()).OrderBy(e => e.EventDate).ToList();

        var model = new DashboardViewModel
        {
            Events = events,
            TotalEnrolments = events.Sum(e => e.EnrolmentCount),
            Upcoming = events.Where(e => e.EventDate >= DateTime.Now && e.Status != "Cancelled").Take(5).ToList()
        };
        return View(model);
    }
}
