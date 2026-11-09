using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    /// <summary>Sends each visitor to the right starting page for their role.</summary>
    public IActionResult Index()
    {
        var session = HttpContext.Session;
        if (!session.IsLoggedIn())
            return RedirectToAction("Login", "Account");

        return session.GetRole() == AppRoles.Organiser
            ? RedirectToAction("Index", "Dashboard")
            : RedirectToAction("Index", "Events");
    }

    public IActionResult ServiceUnavailable() => View();

    public IActionResult StatusPage(int code)
    {
        ViewBag.Code = code;
        return View();
    }

    public IActionResult Error() => View();
}
