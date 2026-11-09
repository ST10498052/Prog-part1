using System.Collections.Concurrent;
using RaceDay.Api.Services;

namespace RaceDay.Tests;

/// <summary>In-memory stand-in for Azure Blob Storage so tests need no Azure account.</summary>
public class FakeBlobStorageService : IBlobStorageService
{
    private readonly ConcurrentDictionary<string, (byte[] Data, string ContentType)> _files = new();

    public async Task UploadAsync(string blobName, Stream content, string contentType)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer);
        _files[blobName] = (buffer.ToArray(), contentType);
    }

    public Task<(Stream Content, string ContentType)?> DownloadAsync(string blobName)
    {
        if (_files.TryGetValue(blobName, out var file))
        {
            Stream stream = new MemoryStream(file.Data);
            return Task.FromResult<(Stream Content, string ContentType)?>((stream, file.ContentType));
        }
        return Task.FromResult<(Stream Content, string ContentType)?>(null);
    }

    public Task DeleteAsync(string blobName)
    {
        _files.TryRemove(blobName, out _);
        return Task.CompletedTask;
    }
}
