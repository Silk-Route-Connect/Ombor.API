using Microsoft.AspNetCore.Http;

namespace Ombor.Application.Extensions;

internal static class FormFileExtensions
{
    public static string GetNormalizedFileExtension(this IFormFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentException.ThrowIfNullOrWhiteSpace(file.FileName);

        var extension = Path.GetExtension(file.FileName);

        return extension.Trim().ToLowerInvariant();
    }
}
