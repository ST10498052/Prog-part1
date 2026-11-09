using System.Net;
using System.Net.Http.Headers;
using static RaceDay.Tests.ApiHelpers;

namespace RaceDay.Tests;

/// <summary>Banner and profile-picture uploads (stored via the blob storage service) and downloads.</summary>
public class ImageTests : IClassFixture<RaceDayFactory>
{
    // Starts with the PNG file signature, which is what the API checks for.
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };

    private readonly RaceDayFactory _factory;

    public ImageTests(RaceDayFactory factory) => _factory = factory;

    private static MultipartFormDataContent FileForm(byte[] bytes, string contentType, string fileName = "image.png")
    {
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        var form = new MultipartFormDataContent();
        form.Add(fileContent, "file", fileName);
        return form;
    }

    [Fact]
    public async Task Organiser_CanUploadBanner_AndItCanBeDownloaded()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);

        var upload = await organiser.PostAsync($"/api/events/{eventId}/banner", FileForm(PngBytes, "image/png"));

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var banner = (await ReadJsonAsync(upload)).GetProperty("bannerImage").GetString();
        Assert.False(string.IsNullOrEmpty(banner));
        Assert.StartsWith("banners/", banner);

        var download = await participant.GetAsync($"/api/files/{banner}");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes, await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Participant_CannotUploadBanner_Returns403()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var participant = await LoggedInClientAsync(_factory, Participant);
        var eventId = await CreateEventAsync(organiser);

        var response = await participant.PostAsync($"/api/events/{eventId}/banner", FileForm(PngBytes, "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CannotUploadBannerForAnotherOrganisersEvent_Returns403()
    {
        var owner = await LoggedInClientAsync(_factory, Organiser);
        var other = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(owner);

        var response = await other.PostAsync($"/api/events/{eventId}/banner", FileForm(PngBytes, "image/png"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithNonImageContentType_Returns400()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner",
            FileForm(System.Text.Encoding.UTF8.GetBytes("not an image"), "text/plain", "notes.txt"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_FileThatIsNotReallyAnImage_Returns400()
    {
        var organiser = await LoggedInClientAsync(_factory, Organiser);
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsync($"/api/events/{eventId}/banner",
            FileForm(System.Text.Encoding.UTF8.GetBytes("this is plain text pretending to be a png"), "image/png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithoutLogin_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsync("/api/events/1/banner", FileForm(PngBytes, "image/png"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task User_CanUploadProfilePicture_AndProfileShowsIt()
    {
        var participant = await LoggedInClientAsync(_factory, Participant);

        var upload = await participant.PostAsync("/api/users/me/profile-picture", FileForm(PngBytes, "image/png"));
        var profile = await ReadJsonAsync(await participant.GetAsync("/api/users/me"));

        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var picture = profile.GetProperty("profilePicture").GetString();
        Assert.False(string.IsNullOrEmpty(picture));
        Assert.StartsWith("profiles/", picture);
    }

    [Fact]
    public async Task Files_WithoutLogin_Returns401()
    {
        var anonymous = _factory.CreateClient();

        var response = await anonymous.GetAsync("/api/files/banners/1-abc.png");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Files_WithUnexpectedPath_Returns404()
    {
        var participant = await LoggedInClientAsync(_factory, Participant);

        var response = await participant.GetAsync("/api/files/secrets/passwords.txt");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
