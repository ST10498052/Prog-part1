using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

/// <summary>
/// Stores images in a PRIVATE Azure Blob Storage container. Images are only ever
/// served back through the API (GET /api/files/...), never by a public Azure URL.
/// </summary>
public class AzureBlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _config;
    private BlobContainerClient? _container;

    public AzureBlobStorageService(IConfiguration config)
    {
        _config = config;
    }

    private async Task<BlobContainerClient> GetContainerAsync()
    {
        if (_container is not null) return _container;

        var connectionString = _config["AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new BlobStorageUnavailableException("Image storage is not configured on the server.");

        var containerName = _config["AzureBlobStorage:ContainerName"];
        if (string.IsNullOrWhiteSpace(containerName)) containerName = "raceday-images";

        try
        {
            var container = new BlobServiceClient(connectionString).GetBlobContainerClient(containerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.None);
            _container = container;
            return container;
        }
        catch (Exception ex)
        {
            throw new BlobStorageUnavailableException("Could not connect to image storage.", ex);
        }
    }

    public async Task UploadAsync(string blobName, Stream content, string contentType)
    {
        var container = await GetContainerAsync();
        try
        {
            await container.GetBlobClient(blobName)
                .UploadAsync(content, new BlobHttpHeaders { ContentType = contentType });
        }
        catch (Exception ex)
        {
            throw new BlobStorageUnavailableException("The image could not be uploaded to storage.", ex);
        }
    }

    public async Task<(Stream Content, string ContentType)?> DownloadAsync(string blobName)
    {
        var container = await GetContainerAsync();
        try
        {
            var blob = container.GetBlobClient(blobName);
            if (!(await blob.ExistsAsync()).Value) return null;

            var result = await blob.DownloadStreamingAsync();
            return (result.Value.Content, result.Value.Details.ContentType);
        }
        catch (Exception ex)
        {
            throw new BlobStorageUnavailableException("The image could not be read from storage.", ex);
        }
    }

    public async Task DeleteAsync(string blobName)
    {
        // Best effort: a failed clean-up of an old image must never break the request.
        try
        {
            var container = await GetContainerAsync();
            await container.GetBlobClient(blobName).DeleteIfExistsAsync();
        }
        catch (Exception)
        {
        }
    }
}
