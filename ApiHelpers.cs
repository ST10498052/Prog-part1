using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Tests;

/// <summary>Small helpers so each test reads like a story.</summary>
public static class ApiHelpers
{
    public const string Password = "Password123!";
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static string NewEmail() => $"{Guid.NewGuid():N}@test.com";

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string role, string? email = null) =>
        client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test " + role,
            email = email ?? NewEmail(),
            password = Password,
            role,
            phoneNumber = "0611122233"
        });

    public static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    /// <summary>Creates a client that is registered and logged in with the given role.</summary>
    public static async Task<HttpClient> LoggedInClientAsync(RaceDayFactory factory, string role)
    {
        var client = factory.CreateClient();
        var email = NewEmail();
        var register = await RegisterAsync(client, role, email);
        register.EnsureSuccessStatusCode();
        var login = await LoginAsync(client, email);
        login.EnsureSuccessStatusCode();
        return client;
    }

    public static object EventBody(string name = "Test Run") => new
    {
        name,
        description = "A test event",
        eventDate = DateTime.UtcNow.AddMonths(2),
        location = "Cape Town",
        distanceKm = 10,
        eventType = "Run"
    };

    public static object CategoryBody(string name = "10km", int maxParticipants = 100) => new
    {
        name,
        description = "Test category",
        distanceKm = 10,
        entryFee = 150,
        maxParticipants
    };

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<int> CreateEventAsync(HttpClient organiser, string name = "Test Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", EventBody(name));
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("eventId").GetInt32();
    }

    public static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, int maxParticipants = 100)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody("10km", maxParticipants));
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("categoryId").GetInt32();
    }

    public static async Task<int> EnrolAsync(HttpClient participant, int eventId, int categoryId)
    {
        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });
        response.EnsureSuccessStatusCode();
        return (await ReadJsonAsync(response)).GetProperty("enrolmentId").GetInt32();
    }
}
