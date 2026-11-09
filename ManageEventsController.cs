using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organisers create, edit and delete their events (including the banner image).</summary>
[AppAuthorize(AppRoles.Organiser)]
public class ManageEventsController : Controller
{
    private readonly ApiClient _api;

    public ManageEventsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EventDto>>("api/events/mine");
        if (!result.Success) ViewBag.Error = result.Message;

        return View((result.Data ?? new List<EventDto>()).OrderByDescending(e => e.EventDate).ToList());
    }

    [HttpGet]
    public IActionResult Create() => View(new EventFormViewModel());

    [HttpPost]
    public async Task<IActionResult> Create(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PostAsync<EventDto>("api/events", ToRequest(model));
        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await UploadBannerIfAnyAsync(result.Data.EventId, model.Banner, "Event created");

        if (TempData.Peek("Warning") is null)
            TempData["Success"] = "Event created. Add categories so participants can enrol.";

        return RedirectToAction("Index", "Categories", new { eventId = result.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _api.GetAsync<EventDto>($"api/events/{id}");
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = string.IsNullOrEmpty(result.Message) ? "That event could not be found." : result.Message;
            return RedirectToAction(nameof(Index));
        }

        var ev = result.Data;
        return View(new EventFormViewModel
        {
            EventId = ev.EventId,
            Name = ev.Name,
            Description = ev.Description,
            EventDate = ev.EventDate,
            Location = ev.Location,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            Status = ev.Status,
            CurrentBanner = ev.BannerImage
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PutAsync<EventDto>($"api/events/{id}", ToRequest(model));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await UploadBannerIfAnyAsync(id, model.Banner, "Event updated");

        if (TempData.Peek("Warning") is null)
            TempData["Success"] = "Event updated.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _api.DeleteAsync($"api/events/{id}");
        if (result.Success)
            TempData["Success"] = "Event deleted.";
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    private async Task UploadBannerIfAnyAsync(int eventId, IFormFile? banner, string savedText)
    {
        if (banner is null || banner.Length == 0) return;

        var upload = await _api.PostFileAsync<EventDto>($"api/events/{eventId}/banner", banner);
        if (!upload.Success)
            TempData["Warning"] = $"{savedText}, but the banner image could not be uploaded: {upload.Message}";
    }

    private static object ToRequest(EventFormViewModel model) => new
    {
        name = model.Name,
        description = model.Description,
        eventDate = model.EventDate,
        location = model.Location,
        distanceKm = model.DistanceKm,
        eventType = model.EventType,
        status = model.Status
    };
}
