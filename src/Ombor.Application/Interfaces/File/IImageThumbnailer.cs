using Ombor.Application.Models;

namespace Ombor.Application.Interfaces.File;

/// <summary>
/// Generates thumbnail images from a source stream.
/// </summary>
public interface IImageThumbnailer
{
    /// <summary>
    /// Produces a resized thumbnail and returns a stream with the image data.
    /// </summary>
    Task<Stream> GenerateThumbnailAsync(Stream source, ThumbnailFormat format, CancellationToken cancellationToken = default);
}
