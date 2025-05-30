using Microsoft.AspNetCore.Hosting;
using Ombor.Application.Interfaces.File;
using Ombor.Infrastructure.Helpers;

namespace Ombor.Infrastructure.Storage;

internal sealed class LocalFileStorage(IWebHostEnvironment env) : IFileStorage
{
    private readonly string _webRootPath = env.WebRootPath
        ?? throw new InvalidOperationException("Cannot construct local file storage without 'web root path'.");

    public async Task SaveAsync(
        Stream content,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var pathSegments = PathHelpers.ExtractSegments(storagePath);
        var relativePhysicalPath = Path.Combine(pathSegments);
        var fullPhysicalPath = Path.Combine(_webRootPath, relativePhysicalPath);

        var directory = Path.GetDirectoryName(fullPhysicalPath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(
            fullPhysicalPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await content.CopyToAsync(fileStream, cancellationToken);
    }
}