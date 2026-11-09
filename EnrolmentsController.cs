using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organisers see who has enrolled for each of their events.</summary>
[AppAuthorize(AppRoles.Organiser)]
public class EnrolmentsController : Controller
{
    private readonly ApiClient _api;

    public EnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(int? eventId)
    {
        var events = await _api.GetAsync<List<EventDto>>("api/events/mine");
        var model = new EnrolmentsPageViewModel { Events = events.Data ?? new List<EventDto>() };
        if (!events.Success) ViewBag.Error = events.Message;

        model.Selected = model.Events.FirstOrDefault(e => e.EventId == eventId) ?? model.Events.FirstOrDefault();
        if (model.Selected is not null)
        {
            var enrolments = await _api.GetAsync<List<EnrolmentDto>>($"api/events/{model.Selected.EventId}/enrolments");
            if (!enrolments.Success) ViewBag.Error = enrolments.Message;
            model.Enrolments = enrolments.Data ?? new List<EnrolmentDto>();
        }
        return View(model);
    }
}
