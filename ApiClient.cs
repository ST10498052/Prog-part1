using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public HttpStatusCode StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
}

/// <summary>Used as the type argument for calls that return no body.</summary>
public sealed class NoContentBody { }

/// <summary>The API says we are not logged in (e.g. the API session expired).</summary>
public class ApiSessionExpiredException : Exception { }

/// <summary>The API says our role is not allowed to do this.</summary>
public class ApiForbiddenException : Exception { }

/// <summary>The API could not be reached or returned something unusable.</summary>
public class ApiUnavailableException : Exception
{
    public ApiUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}

/// <summary>
/// Talks to the RaceDay API. The API identifies users with a session cookie, so after login
/// we keep that cookie in THIS app's session and send it back on every call.
/// </summary>
public class ApiClient
{
    private const string ApiCookieName = ".RaceDay.Session";
    private const string ApiCookieSessionKey = "ApiCookie";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    public Task<ApiResponse<T>> GetAsync<T>(string path) =>
        SendAsync<T>(HttpMethod.Get, path, null, false);

    public Task<ApiResponse<T>> PostAsync<T>(string path, object? body = null, bool unauthorizedIsFailure = false) =>
        SendAsync<T>(HttpMethod.Post, path, ToJson(body), unauthorizedIsFailure);

    public Task<ApiResponse<T>> PutAsync<T>(string path, object body) =>
        SendAsync<T>(HttpMethod.Put, path, ToJson(body), false);

    public Task<ApiResponse<NoContentBody>> DeleteAsync(string path) =>
        SendAsync<NoContentBody>(HttpMethod.Delete, path, null, false);

    /// <summary>Uploads an image to an API endpoint that expects a multipart field called "file".</summary>
    public Task<ApiResponse<T>> PostFileAsync<T>(string path, IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.TryParse(file.ContentType, out var mediaType)
            ? mediaType
            : new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", Path.GetFileName(file.FileName));
        return SendAsync<T>(HttpMethod.Post, path, form, false);
    }

    /// <summary>Downloads an image through the API. Returns null if it is not available.</summary>
    public async Task<(byte[] Data, string ContentType)?> GetFileAsync(string blobName)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/files/" + blobName);
            AddCookie(request);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var data = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            return (data, contentType);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static HttpContent? ToJson(object? body) =>
        body is null ? null : JsonContent.Create(body, body.GetType(), null, Json);

    private void AddCookie(HttpRequestMessage request)
    {
        var cookie = Session.GetString(ApiCookieSessionKey);
        if (!string.IsNullOrEmpty(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);
    }

    private void CaptureCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return;

        foreach (var value in values)
        {
            if (value.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
                Session.SetString(ApiCookieSessionKey, value.Split(';')[0]);
        }
    }

    private async Task<ApiResponse<T>> SendAsync<T>(HttpMethod method, string path, HttpContent? content, bool unauthorizedIsFailure)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        AddCookie(request);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            throw new ApiUnavailableException("The RaceDay service could not be reached.", ex);
        }

        using (response)
        {
            CaptureCookie(response);

            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new ApiForbiddenException();
            if (response.StatusCode == HttpStatusCode.Unauthorized && !unauthorizedIsFailure)
                throw new ApiSessionExpiredException();

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResponse<T>
                {
                    Success = false,
                    StatusCode = response.StatusCode,
                    Message = await ReadErrorAsync(response)
                };
            }

            T? data = default;
            if (typeof(T) != typeof(NoContentBody) && response.StatusCode != HttpStatusCode.NoContent)
            {
                try
                {
                    data = await response.Content.ReadFromJsonAsync<T>(Json);
                }
                catch (JsonException ex)
                {
                    throw new ApiUnavailableException("The RaceDay service returned an unexpected response.", ex);
                }
            }

            return new ApiResponse<T> { Success = true, StatusCode = response.StatusCode, Data = data };
        }
    }

    /// <summary>Pulls a readable message out of the API's error JSON.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                        return message.GetString() ?? string.Empty;

                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = new List<string>();
                        foreach (var field in errors.EnumerateObject())
                        {
                            if (field.Value.ValueKind != JsonValueKind.Array) continue;
                            foreach (var item in field.Value.EnumerateArray())
                            {
                                var line = item.GetString();
                                if (!string.IsNullOrWhiteSpace(line)) messages.Add(line);
                            }
                        }
                        if (messages.Count > 0) return string.Join(" ", messages);
                    }
                }
            }
        }
        catch (JsonException)
        {
        }

        return response.StatusCode switch
        {
            HttpStatusCode.NotFound => "That item could not be found.",
            HttpStatusCode.Conflict => "That action conflicts with existing data.",
            HttpStatusCode.BadRequest => "Some of the information was not valid.",
            _ => "Something went wrong. Please try again."
        };
    }
}
