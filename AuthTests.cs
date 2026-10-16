using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using static RaceDay.Tests.ApiHelpers;

namespace RaceDay.Tests;

/// <summary>User registration, login, logout and the "must be logged in" rule.</summary>
public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_ValidOrganiser_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await RegisterAsync(client, Organiser);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(Organiser, json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Register_ValidParticipant_Returns201()
    {
        var client = _factory.CreateClient();
        var response = await RegisterAsync(client, Participant);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(Participant, json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await RegisterAsync(client, Participant, email);

        var second = await RegisterAsync(client, Participant, email);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRole_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await RegisterAsync(client, "Admin");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Short Pass",
            email = NewEmail(),
            password = "abc",
            role = Participant
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_PasswordIsStoredHashed_NotPlainText()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await RegisterAsync(client, Participant, email);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var user = db.Users.Single(u => u.Email == email);

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.DoesNotContain(Password, user.PasswordHash);
    }

    [Fact]
    public async Task Login_CorrectCredentials_Returns200AndRole()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await RegisterAsync(client, Organiser, email);

        var response = await LoginAsync(client, email);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(Organiser, json.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        var email = NewEmail();
        await RegisterAsync(client, Participant, email);

        var response = await LoginAsync(client, email, "WrongPassword!");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await LoginAsync(client, NewEmail());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoints_WithoutLogin_Return401()
    {
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/events")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/users/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/events/1/categories")).StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AfterLogin_Returns200()
    {
        var client = await LoggedInClientAsync(_factory, Participant);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_EndsSession_SoProtectedEndpointReturns401()
    {
        var client = await LoggedInClientAsync(_factory, Participant);

        var logout = await client.PostAsync("/api/auth/logout", null);
        var after = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, after.StatusCode);
    }

    [Fact]
    public async Task Profile_CanBeViewedAndUpdated_ByLoggedInUser()
    {
        var client = await LoggedInClientAsync(_factory, Participant);

        var update = await client.PutAsJsonAsync("/api/users/me", new { fullName = "Updated Name", phoneNumber = "0821234567" });
        var view = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var json = await ReadJsonAsync(view);
        Assert.Equal("Updated Name", json.GetProperty("fullName").GetString());
    }
}
