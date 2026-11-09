using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Serves stored images (event banners and profile pictures) to logged-in users.</summary>
/// <remarks>The storage container is private, so every image is streamed through the API.</remarks>
[ApiController]
[Route("api/files")]
[SessionAuthorize]
public class FilesController : ControllerBase
{
    private static readonly Regex AllowedName = new(@"^(banners|profiles)/[A-Za-z0-9._-]+$", RegexOptions.Compiled);

    private readonly IBlobStorageService _blobs;

    public FilesController(IBlobStorageService blobs)
    {
        _blobs = blobs;
    }

    /// <summary>Download an image by its blob name, e.g. banners/12-abc.jpg.</summary>
    /// <param name="blobName">The BannerImage or ProfilePicture value returned by the API.</param>
    /// <response code="200">The image file.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="404">No such image.</response>
    /// <response code="503">Image storage is not available.</response>
    [HttpGet("{*blobName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Download(string blobName)
    {
        if (!AllowedName.IsMatch(blobName))
            return NotFound(new { message = "Image not found." });

        try
        {
            var file = await _blobs.DownloadAsync(blobName);
            if (file is null) return NotFound(new { message = "Image not found." });

            Response.Headers.CacheControl = "private, max-age=600";
            return File(file.Value.Content, file.Value.ContentType);
        }
        catch (BlobStorageUnavailableException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = ex.Message });
        }
    }
}
