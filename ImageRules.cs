namespace RaceDay.Api.Services;

/// <summary>Validation for uploaded images (type, size and file signature).</summary>
public static class ImageRules
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/webp"] = ".webp"
    };

    /// <summary>Returns an error message (or null if the file is fine) and the file extension to use.</summary>
    public static async Task<(string? Error, string Extension)> ValidateAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return ("Please choose an image file to upload.", string.Empty);
        if (file.Length > MaxBytes)
            return ("The image must be 5 MB or smaller.", string.Empty);
        if (!Extensions.TryGetValue(file.ContentType ?? string.Empty, out var extension))
            return ("Only JPEG, PNG, GIF or WebP images are allowed.", string.Empty);
        if (!await LooksLikeImageAsync(file))
            return ("That file does not look like a valid image.", string.Empty);

        return (null, extension);
    }

    private static async Task<bool> LooksLikeImageAsync(IFormFile file)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header, 0, header.Length);
        if (read < 4) return false;

        var jpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var png = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var gif = header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38;
        var webp = read >= 12
                   && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                   && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        return jpeg || png || gif || webp;
    }
}
