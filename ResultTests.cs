using System.Net;
using static RaceDay.Tests.ApiHelpers;

namespace RaceDay.Tests;

/// <summary>Organisers capturing results; Participants viewing their own.</summary>
public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory) => _factory = factory;

    private static object FinishedBody(string time = "01:52:30", int position = 3) =>
        new { finishTime = time, position, status = "Finished" };

    [Fact]
    public async Task Organiser_CanCaptureResult_Returns201()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await EnrolAsync(participant, eventId, categoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedBody());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var json = await ReadJsonAsync(response);
        Assert.Equal(3, json.GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await EnrolAsync(participant, eventId, categoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedBody());

        var second = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedBody());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutTime_Returns400()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await EnrolAsync(participant, eventId, categoryId);

        var response = await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result",
            new { status = "Finished" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewOwnResult()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await EnrolAsync(participant, eventId, categoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedBody("00:45:10", 7));

        var response = await participant.GetAsync("/api/users/me/results");
        var json = await ReadJsonAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, json.GetArrayLength());
        Assert.Equal(7, json[0].GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Participant_DoesNotSeeOtherParticipantsResultsInTheirOwnList()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var first = await LoggedInClientAsync(_factory, Participant);
        var second = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var firstEnrolment = await EnrolAsync(first, eventId, categoryId);
        await EnrolAsync(second, eventId, categoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{firstEnrolment}/result", FinishedBody());

        var json = await ReadJsonAsync(await second.GetAsync("/api/users/me/results"));

        Assert.Equal(0, json.GetArrayLength());
    }

    [Fact]
    public async Task Organiser_CanCorrectResult_Returns200()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolmentId = await EnrolAsync(participant, eventId, categoryId);
        var created = await ReadJsonAsync(await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedBody()));
        var resultId = created.GetProperty("resultId").GetInt32();

        var update = await organiser.PutAsJsonAsync($"/api/results/{resultId}", FinishedBody("01:40:00", 1));

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal(1, (await ReadJsonAsync(update)).GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task Organiser_CannotCaptureResultForAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClientAsync(_factory, Organiser);
        var other = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(owner);
        var categoryId = await CreateCategoryAsync(owner, eventId);
        var enrolmentId = await EnrolAsync(participant, eventId, categoryId);

        var response = await other.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", FinishedBody());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task EventResults_AreOrderedByPosition()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var first = await LoggedInClientAsync(_factory, Participant);
        var second = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);
        var enrolA = await EnrolAsync(first, eventId, categoryId);
        var enrolB = await EnrolAsync(second, eventId, categoryId);
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolA}/result", FinishedBody("02:00:00", 2));
        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolB}/result", FinishedBody("01:50:00", 1));

        var json = await ReadJsonAsync(await first.GetAsync($"/api/events/{eventId}/results"));

        Assert.Equal(2, json.GetArrayLength());
        Assert.Equal(1, json[0].GetProperty("position").GetInt32());
        Assert.Equal(2, json[1].GetProperty("position").GetInt32());
    }
}
