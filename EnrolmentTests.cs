using System.Net;
using static RaceDay.Tests.ApiHelpers;

namespace RaceDay.Tests;

/// <summary>Participants enrolling, and the enrolment being recorded correctly.</summary>
public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CanEnrol_Returns201WithBibNumber()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("bibNumber").GetString()));
        Assert.Equal("Confirmed", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Enrolment_RecordsParticipantEventAndCategory()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser, "Link Test");
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        await EnrolAsync(participant, eventId, categoryId);

        // The participant sees the link in their own history...
        var mine = await ReadJsonAsync(await participant.GetAsync("/api/users/me/enrolments"));
        Assert.Equal(1, mine.GetArrayLength());
        Assert.Equal(eventId, mine[0].GetProperty("eventId").GetInt32());
        Assert.Equal(categoryId, mine[0].GetProperty("categoryId").GetInt32());
        Assert.Equal("Link Test", mine[0].GetProperty("eventName").GetString());

        // ...and the organiser sees the same enrolment for their event.
        var forEvent = await ReadJsonAsync(await organiser.GetAsync($"/api/events/{eventId}/enrolments"));
        Assert.Equal(1, forEvent.GetArrayLength());
        Assert.Equal(categoryId, forEvent[0].GetProperty("categoryId").GetInt32());
        Assert.Equal(mine[0].GetProperty("participantId").GetInt32(), forEvent[0].GetProperty("participantId").GetInt32());
    }

    [Fact]
    public async Task Participant_CannotEnrolTwiceInSameEvent_Returns409()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        await EnrolAsync(participant, eventId, categoryId);

        var second = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns409()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var first = await LoggedInClientAsync(_factory, Participant);
        var second = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId, maxParticipants: 1);
        await EnrolAsync(first, eventId, categoryId);

        var response = await second.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_Returns404()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventA = await CreateEventAsync(organiser, "Event A");
        var eventB = await CreateEventAsync(organiser, "Event B");
        var categoryOfB = await CreateCategoryAsync(organiser, eventB);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventA}/enrolments", new { categoryId = categoryOfB });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InMissingEvent_Returns404()
    {
        var participant = await LoggedInClientAsync(_factory, Participant);

        var response = await participant.PostAsJsonAsync("/api/events/999999/enrolments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InCancelledEvent_Returns409()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        await organiser.PutAsJsonAsync($"/api/events/{eventId}", new
        {
            name = "Cancelled",
            eventDate = DateTime.UtcNow.AddMonths(1),
            location = "Cape Town",
            distanceKm = 10,
            eventType = "Run",
            status = "Cancelled"
        });

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanWithdraw_ThenHistoryIsEmpty()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await EnrolAsync(participant, eventId, categoryId);

        var delete = await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");
        var mine = await ReadJsonAsync(await participant.GetAsync("/api/users/me/enrolments"));

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(0, mine.GetArrayLength());
    }

    [Fact]
    public async Task Participant_CannotWithdrawSomeoneElsesEnrolment_Returns403()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var owner = await LoggedInClientAsync(_factory, Participant);
        var other = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await EnrolAsync(owner, eventId, categoryId);

        var response = await other.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotViewEnrolmentsOfAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClientAsync(_factory, Organiser);
        var other = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(owner);

        var response = await other.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
