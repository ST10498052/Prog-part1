using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>A Participant's personal race history.</summary>
[AppAuthorize(AppRoles.Participant)]
public class MyResultsController : Controller
{
    private readonly ApiClient _api;

    public MyResultsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<List<ResultDto>>("api/users/me/results");
        if (!result.Success) ViewBag.Error = result.Message;

        var list = (result.Data ?? new List<ResultDto>()).OrderByDescending(r => r.EventDate).ToList();
        return View(list);
    }
}
