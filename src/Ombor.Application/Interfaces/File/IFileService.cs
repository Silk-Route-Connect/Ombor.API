using Microsoft.AspNetCore.Http;
using Ombor.Application.Models;

namespace Ombor.Application.Interfaces.File;

/// <summary>
/// Orchestrates validation, storage, and thumbnailing of uploaded files. Every upload is checked by content (magic
/// bytes) against its extension and stored under a random (GUID) name, so its public URL cannot be guessed.
/// </summary>
public interface IFileService
{
    /// <summary>Uploads a document attachment: any allowed file type (images or PDF).</summary>
    Task<FileUploadResult> UploadAsync(IFormFile file, string? subfolder = null, CancellationToken cancellationToken = default);

    /// <summary>Uploads an image (product image, organization logo): image types only.</summary>
    Task<FileUploadResult> UploadImageAsync(IFormFile file, string? subfolder = null, CancellationToken cancellationToken = default);

    /// <summary>Uploads several images (product images): image types only.</summary>
    Task<FileUploadResult[]> UploadImagesAsync(IEnumerable<IFormFile> files, string? subfolder = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(string fileName, string? subfolder = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(IEnumerable<string> fileNames, string? subfolder = null, CancellationToken cancellationToken = default);
}
