
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Api.Services;

/// <summary>
/// Session-based authorization.
/// Any logged-in user is allowed when no roles are specified.
/// When roles are specified, the user's session role must match.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class SessionAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string[] _roles;

    public SessionAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public async Task OnAuthorizationAsync(
        AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        await httpContext.Session.LoadAsync();

        var userId = httpContext.GetUserId();
        var role = httpContext.GetRole();

        if (userId is null || string.IsNullOrWhiteSpace(role))
        {
            context.Result = new ObjectResult(new
            {
                message = "You must be logged in to access this resource."
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };

            return;
        }

        if (_roles.Length > 0 &&
            !_roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            context.Result = new ObjectResult(new
            {
                message = $"This action is only available to: {string.Join(", ", _roles)}."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
