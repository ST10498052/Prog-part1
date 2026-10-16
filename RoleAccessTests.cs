using System.Net;
using static RaceDay.Tests.ApiHelpers;

namespace RaceDay.Tests;

/// <summary>Organiser-only and Participant-only functionality must reject the other role (403) and anonymous users (401).</summary>
public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory) => _factory = factory;

    [Fact]
    public async Task Participant_CannotCreateEvent_Returns403()
    {
        var participant = await LoggedInClientAsync(_factory, Participant);

        var response = await participant.PostAsJsonAsync("/api/events", EventBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUpdateOrDeleteEvent_Returns403()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);

        var update = await participant.PutAsJsonAsync($"/api/events/{eventId}", EventBody("Nope"));
        var delete = await participant.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCreateCategory_Returns403()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", CategoryBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotViewEventEnrolments_Returns403()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);

        var response = await participant.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotCaptureResult_Returns403()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await EnrolAsync(participant, eventId, categoryId);

        var response = await participant.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { finishTime = "01:00:00", position = 1, status = "Finished" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotEnrolInEvent_Returns403()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/enrolments", new { categoryId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUseParticipantOnlyEndpoints_Returns403()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);

        var enrolments = await organiser.GetAsync("/api/users/me/enrolments");
        var results = await organiser.GetAsync("/api/users/me/results");

        Assert.Equal(HttpStatusCode.Forbidden, enrolments.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, results.StatusCode);
    }

    [Fact]
    public async Task Participant_CannotUseOrganiserOnlyList_Returns403()
    {
        var participant = await LoggedInClientAsync(_factory, Participant);

        var response = await participant.GetAsync("/api/events/mine");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_CannotCreateEventOrEnrol_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var create = await anonymous.PostAsJsonAsync("/api/events", EventBody());
        var enrol = await anonymous.PostAsJsonAsync("/api/events/1/enrolments", new { categoryId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, enrol.StatusCode);
    }
}
