using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects MVC pages. Not logged in: redirect to the login page.
/// Logged in with the wrong role: redirect to the access denied page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AppAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public AppAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var session = context.HttpContext.Session;

        if (!session.IsLoggedIn())
        {
            var request = context.HttpContext.Request;
            context.Result = new RedirectToActionResult("Login", "Account",
                new { returnUrl = request.Path + request.QueryString });
            return;
        }

        var role = session.GetRole();
        if (_roles.Length > 0 && (role is null || !_roles.Contains(role)))
            context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
    }
}
