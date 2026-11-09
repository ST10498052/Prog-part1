namespace RaceDay.Api.Services;

/// <summary>
/// File storage used by the API. The MVC app never talks to storage directly;
/// it always goes through the API endpoints that use this service.
/// </summary>
public interface IBlobStorageService
{
    Task UploadAsync(string blobName, Stream content, string contentType);

    /// <summary>Returns the file, or null if it does not exist.</summary>
    Task<(Stream Content, string ContentType)?> DownloadAsync(string blobName);

    Task DeleteAsync(string blobName);
}

/// <summary>Thrown when image storage is not configured or Azure cannot be reached.</summary>
public class BlobStorageUnavailableException : Exception
{
    public BlobStorageUnavailableException(string message, Exception? inner = null) : base(message, inner) { }
}
