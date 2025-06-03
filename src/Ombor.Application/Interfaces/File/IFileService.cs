using Microsoft.AspNetCore.Http;
using Ombor.Application.Models;

namespace Ombor.Application.Interfaces.File;

/// <summary>
/// Orchestrates validation, storage, and thumbnailing of uploaded files.
/// </summary>
public interface IFileService
{
    Task<FileUploadResult> UploadAsync(IFormFile file, string? subfolder = null, CancellationToken cancellationToken = default);

    Task<FileUploadResult[]> UploadAsync(IEnumerable<IFormFile> files, string? subfolder = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(string fileName, string? subfolder = null, CancellationToken cancellationToken = default);

    Task DeleteAsync(IEnumerable<string> fileNames, string? subfolder = null, CancellationToken cancellationToken = default);
}
