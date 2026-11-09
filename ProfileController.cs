using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>View and update personal details and upload a profile picture (both roles).</summary>
[AppAuthorize]
public class ProfileController : Controller
{
    private readonly ApiClient _api;

    public ProfileController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await _api.GetAsync<UserProfileDto>("api/users/me");
        if (!result.Success || result.Data is null)
        {
            TempData["Error"] = string.IsNullOrEmpty(result.Message) ? "Your profile could not be loaded." : result.Message;
            return RedirectToAction("Index", "Home");
        }

        return View(ToViewModel(result.Data));
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
            return await ShowWithDetailsAsync(model);

        var result = await _api.PutAsync<UserProfileDto>("api/users/me", new
        {
            fullName = model.FullName,
            phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await ShowWithDetailsAsync(model);
        }

        HttpContext.Session.UpdateName(model.FullName.Trim());
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture is null || picture.Length == 0)
        {
            TempData["Error"] = "Please choose an image first.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _api.PostFileAsync<UserProfileDto>("api/users/me/profile-picture", picture);
        if (result.Success)
            TempData["Success"] = "Your profile picture has been updated.";
        else
            TempData["Error"] = result.Message;

        return RedirectToAction(nameof(Index));
    }

    // When validation fails we still need the read-only details (email, role, picture) from the API.
    private async Task<IActionResult> ShowWithDetailsAsync(ProfileViewModel model)
    {
        var current = await _api.GetAsync<UserProfileDto>("api/users/me");
        if (current.Data is not null)
        {
            model.Email = current.Data.Email;
            model.Role = current.Data.Role;
            model.ProfilePicture = current.Data.ProfilePicture;
        }
        return View("Index", model);
    }

    private static ProfileViewModel ToViewModel(UserProfileDto user) => new()
    {
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        Email = user.Email,
        Role = user.Role,
        ProfilePicture = user.ProfilePicture
    };
}
