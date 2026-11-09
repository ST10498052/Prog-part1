using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>A Participant's own enrolments and their statuses.</summary>
[AppAuthorize(AppRoles.Participant)]
public class MyEnrolmentsController : Controller
{
    private readonly ApiClient _api;

    public MyEnrolmentsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<EnrolmentDto>>("api/users/me/enrolments");
        if (!result.Success) ViewBag.Error = result.Message;

        var list = (result.Data ?? new List<EnrolmentDto>()).OrderBy(e => e.EventDate).ToList();
        return View(list);
    }

    [HttpPost]
    public async Task<IActionResult> Withdraw(int id)
    {
        var result = await _api.DeleteAsync($"api/enrolments/{id}");
        if (result.Success)
            TempData["Success"] = "You have been withdrawn from the event.";
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }
}
