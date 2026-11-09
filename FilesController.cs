using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Shows event banners and profile pictures. The browser asks this app, and this app asks the
/// API for the image, so the MVC app never talks to Azure directly.
/// </summary>
[AppAuthorize]
public class FilesController : Controller
{
    private readonly ApiClient _api;

    public FilesController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet("/files/{*name}")]
    public async Task<IActionResult> Image(string name)
    {
        var file = await _api.GetFileAsync(name);
        if (file is null) return NotFound();

        Response.Headers.CacheControl = "private, max-age=600";
        return File(file.Value.Data, file.Value.ContentType);
    }
}
