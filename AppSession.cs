using RaceDay.Web.Models;

namespace RaceDay.Web.Services;

public static class AppRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

/// <summary>Helpers for the logged-in user stored in this MVC app's session.</summary>
public static class AppSession
{
    private const string UserIdKey = "UserId";
    private const string RoleKey = "Role";
    private const string NameKey = "FullName";

    public static bool IsLoggedIn(this ISession session) => session.GetInt32(UserIdKey).HasValue;

    public static string? GetRole(this ISession session) => session.GetString(RoleKey);

    public static string GetFullName(this ISession session) => session.GetString(NameKey) ?? string.Empty;

    public static void SignIn(this ISession session, UserProfileDto user)
    {
        session.SetInt32(UserIdKey, user.UserId);
        session.SetString(RoleKey, user.Role);
        session.SetString(NameKey, user.FullName);
    }

    public static void UpdateName(this ISession session, string fullName) => session.SetString(NameKey, fullName);
}
