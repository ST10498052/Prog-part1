using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Services;

/// <summary>Session keys and helpers for reading the logged-in user.</summary>
public static class SessionExtensions
{
    public const string UserIdKey = "UserId";
    public const string RoleKey = "Role";

    public static int? GetUserId(this HttpContext context) => context.Session.GetInt32(UserIdKey);
    public static string? GetRole(this HttpContext context) => context.Session.GetString(RoleKey);
}

/// <summary>
/// Session-based authorization. With no roles: any logged-in user is allowed.
/// With roles: the session role must match one of them.
/// Not logged in = 401 Unauthorized. Wrong role = 403 Forbidden.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        await context.HttpContext.Session.LoadAsync();

        var userId = context.HttpContext.GetUserId();
        var role = context.HttpContext.GetRole();

        if (userId is null || role is null)
        {
            context.Result = new ObjectResult(new { message = "You must be logged in to access this resource." })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
            return;
        }

        if (_roles.Length > 0 && !_roles.Contains(role))
        {
            context.Result = new ObjectResult(new { message = $"This action is only available to: {string.Join(", ", _roles)}." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
