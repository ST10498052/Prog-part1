using System.Net;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in and log out. All of it goes through the Part 2 API.</summary>
public class AccountController : Controller
{
    private readonly ApiClient _api;

    public AccountController(ApiClient api)
    {
        _api = api;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool expired = false)
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToAction("Index", "Home");

        if (expired)
            ViewBag.Notice = "Your session has expired. Please log in again.";

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PostAsync<UserProfileDto>(
            "api/auth/login",
            new { email = model.Email, password = model.Password },
            unauthorizedIsFailure: true);

        if (!result.Success || result.Data is null)
        {
            var message = result.StatusCode == HttpStatusCode.Unauthorized
                ? "Invalid email or password."
                : result.Message;
            ModelState.AddModelError(string.Empty, message);
            return View(model);
        }

        HttpContext.Session.SignIn(result.Data);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return LocalRedirect(model.ReturnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsLoggedIn())
            return RedirectToAction("Index", "Home");

        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _api.PostAsync<UserProfileDto>("api/auth/register", new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            phoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = "Your account has been created. Please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        try
        {
            await _api.PostAsync<NoContentBody>("api/auth/logout");
        }
        catch (ApiSessionExpiredException)
        {
        }
        catch (ApiUnavailableException)
        {
        }
        catch (ApiForbiddenException)
        {
        }

        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
