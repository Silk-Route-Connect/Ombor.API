namespace Ombor.Application.Interfaces.File;

/// <summary>
/// Abstracts storage of file streams and returns public URLs.
/// </summary>
public interface IFileStorage
{
    /// <summary>
    /// Uploads the given stream to the specified storage path and returns its public URL.
    /// </summary>
    Task SaveAsync(Stream content, string storagePath, CancellationToken cancellationToken = default);
}
