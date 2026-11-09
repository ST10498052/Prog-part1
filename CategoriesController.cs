using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Organisers add age or distance categories to their events.</summary>
[AppAuthorize(AppRoles.Organiser)]
public class CategoriesController : Controller
{
    private readonly ApiClient _api;

    public CategoriesController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(int? eventId)
    {
        var events = await _api.GetAsync<List<EventDto>>("api/events/mine");
        var model = new CategoriesPageViewModel { Events = events.Data ?? new List<EventDto>() };
        if (!events.Success) ViewBag.Error = events.Message;

        model.Selected = model.Events.FirstOrDefault(e => e.EventId == eventId) ?? model.Events.FirstOrDefault();
        if (model.Selected is not null)
        {
            var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{model.Selected.EventId}/categories");
            model.Categories = categories.Data ?? new List<CategoryDto>();
            model.NewCategory.EventId = model.Selected.EventId;
        }
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstErrors();
            return RedirectToAction(nameof(Index), new { eventId = model.EventId });
        }

        var result = await _api.PostAsync<CategoryDto>($"api/events/{model.EventId}/categories", ToRequest(model));
        if (result.Success)
            TempData["Success"] = $"Category \"{model.Name}\" added.";
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, int eventId)
    {
        var categories = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
        var category = categories.Data?.FirstOrDefault(c => c.CategoryId == id);
        if (category is null)
        {
            TempData["Error"] = "That category could not be found.";
            return RedirectToAction(nameof(Index), new { eventId });
        }

        return View(new CategoryFormViewModel
        {
            CategoryId = category.CategoryId,
            EventId = category.EventId,
            Name = category.Name,
            Description = category.Description,
            MinAge = category.MinAge,
            MaxAge = category.MaxAge,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel model)
    {
        model.CategoryId = id;
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PutAsync<CategoryDto>($"api/categories/{id}", ToRequest(model));
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { eventId = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, int eventId)
    {
        var result = await _api.DeleteAsync($"api/categories/{id}");
        if (result.Success)
            TempData["Success"] = "Category deleted.";
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index), new { eventId });
    }

    private string FirstErrors()
    {
        var messages = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct();
        var text = string.Join(" ", messages);
        return string.IsNullOrEmpty(text) ? "Some of the category details were not valid." : text;
    }

    private static object ToRequest(CategoryFormViewModel model) => new
    {
        name = model.Name,
        description = model.Description,
        minAge = model.MinAge,
        maxAge = model.MaxAge,
        distanceKm = model.DistanceKm,
        entryFee = model.EntryFee,
        maxParticipants = model.MaxParticipants
    };
}
