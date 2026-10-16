using System.Net;
using static RaceDay.Tests.ApiHelpers;

namespace RaceDay.Tests;

/// <summary>Organisers managing events, and both roles viewing them.</summary>
public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Organiser_CanCreateEvent_Returns201()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);

        var response = await organiser.PostAsJsonAsync("/api/events", EventBody("Create Test"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("Create Test", json.GetProperty("name").GetString());
        Assert.Equal("Run", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CreateEventWithInvalidType_Returns400()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);

        var response = await organiser.PostAsJsonAsync("/api/events", new
        {
            name = "Bad Type",
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Durban",
            distanceKm = 5,
            eventType = "Swim"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanUpdateOwnEvent_Returns200()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(organiser, "Before");

        var response = await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            name = "After",
            description = "Updated",
            eventDate = DateTime.UtcNow.AddMonths(3),
            location = "Pretoria",
            distanceKm = 21.1,
            eventType = "Walk"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal("After", json.GetProperty("name").GetString());
        Assert.Equal("Walk", json.GetProperty("eventType").GetString());
    }

    [Fact]
    public async Task Organiser_CanDeleteOwnEvent_Returns204_ThenEventIsGone()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(organiser);

        var delete = await organiser.DeleteAsync($"/api/events/{eventId}");
        var get = await organiser.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEditAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClientAsync(_factory, Organiser);
        var other = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(owner);

        var update = await other.PutAsJsonAsync($"/api/events/{eventId}", EventBody("Hijacked"));
        var delete = await other.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdatingMissingEvent_Returns404()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);

        var response = await organiser.PutAsJsonAsync("/api/events/999999", EventBody());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task BothRoles_CanViewEvents()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);

        var asOrganiser = await organiser.GetAsync("/api/events");
        var asParticipant = await participant.GetAsync("/api/events");
        var single = await participant.GetAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.OK, asOrganiser.StatusCode);
        Assert.Equal(HttpStatusCode.OK, asParticipant.StatusCode);
        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
    }

    [Fact]
    public async Task Organiser_Mine_ReturnsOnlyOwnEvents()
    {
        var organiserA = await LoggedInClientAsync(_factory, Organiser);
        var organiserB = await LoggedInClientAsync(_factory, Organiser);
        await CreateEventAsync(organiserA, "A's event");
        await CreateEventAsync(organiserB, "B's event");

        var response = await organiserA.GetAsync("/api/events/mine");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, json.GetArrayLength());
        Assert.Equal("A's event", json[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Organiser_CanManageCategories_ForOwnEvent()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(organiser);

        var create = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody("Under 20"));
        var categoryId = (await ReadJsonAsync(create)).GetProperty("categoryId").GetInt32();
        var update = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}", CategoryBody("Senior"));
        var list = await organiser.GetAsync($"/api/events/{eventId}/categories");
        var delete = await organiser.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task Category_MinAgeGreaterThanMaxAge_Returns400()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new
        {
            name = "Broken",
            minAge = 40,
            maxAge = 20,
            entryFee = 100,
            maxParticipants = 50
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotDeleteEventThatHasEnrolments_Returns409()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        await EnrolAsync(participant, eventId, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
