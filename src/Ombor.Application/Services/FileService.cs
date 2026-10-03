using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Extensions;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Models;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class FileService(
    IFileStorage storage,
    IImageThumbnailer thumbnailer,
    IFilePathProvider pathResolver,
    IOptions<FileSettings> options,
    ILogger<FileService> logger) : IFileService
{
    private readonly FileSettings _settings = options.Value;

    public Task<FileUploadResult> UploadAsync(
        IFormFile file,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
        => UploadCheckedAsync(file, subfolder, imagesOnly: false, cancellationToken);

    public Task<FileUploadResult> UploadImageAsync(
        IFormFile file,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
        => UploadCheckedAsync(file, subfolder, imagesOnly: true, cancellationToken);

    public async Task<FileUploadResult[]> UploadImagesAsync(
        IEnumerable<IFormFile> files,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(files);

        var results = new List<FileUploadResult>();
        foreach (var file in files)
        {
            results.Add(await UploadImageAsync(file, subfolder, cancellationToken));
        }

        return [.. results];
    }

    private async Task<FileUploadResult> UploadCheckedAsync(
        IFormFile file,
        string? subfolder,
        bool imagesOnly,
        CancellationToken cancellationToken)
    {
        var contentType = await ValidateOrThrowAsync(file, imagesOnly, cancellationToken);

        var extension = file.GetNormalizedFileExtension();
        var generatedFileName = $"{Guid.NewGuid():N}{extension}";

        var originalUrl = await SaveOriginalFileAsync(
            file: file,
            fileName: generatedFileName,
            subfolder: subfolder,
            cancellationToken: cancellationToken);
        var thumbnailUrl = await TrySaveThumbnailAsync(
            file: file,
            fileName: generatedFileName,
            extension: extension,
            subfolder: subfolder,
            cancellationToken: cancellationToken);

        return new FileUploadResult(
            FileName: generatedFileName,
            OriginalFileName: file.FileName,
            Url: originalUrl,
            ThumbnailUrl: thumbnailUrl,
            ContentType: contentType);
    }

    public async Task DeleteAsync(
        string fileName,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        await DeleteOriginalAsync(fileName, subfolder, cancellationToken);
        await TryDeleteThumbnailAsync(fileName, subfolder, cancellationToken);
    }

    public Task DeleteAsync(
        IEnumerable<string> fileNames,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileNames);

        var tasks = fileNames.Select(fileName => DeleteAsync(fileName, subfolder, cancellationToken));

        return Task.WhenAll(tasks);
    }

    /// <summary>
    /// Rejects an empty, oversized or disallowed upload, and one whose bytes are not what its extension claims
    /// (a renamed executable, HTML or SVG never reaches storage). Returns the content type the bytes prove.
    /// </summary>
    private async Task<string> ValidateOrThrowAsync(IFormFile file, bool imagesOnly, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new InvalidFileContentException("File is required.");
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            throw new InvalidFileContentException("File name is required.");
        }

        if (file.Length > _settings.MaxBytes)
        {
            throw new FileTooLargeException(file.Length, _settings.MaxBytes);
        }

        var extension = file.GetNormalizedFileExtension();
        var allowed = imagesOnly ? _settings.AllowedImageExtensions : _settings.AllowedFileExtensions;
        if (!allowed.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnsupportedFileFormatException(extension, allowed);
        }

        var header = new byte[FileSignatures.HeaderLength];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        var detected = FileSignatures.Detect(header.AsSpan(0, read));

        if (detected is null || detected != FileSignatures.ExpectedFor(extension))
        {
            throw new InvalidFileContentException(
                $"The content of '{file.FileName}' is not a valid {extension} file (detected: {detected ?? "unknown"}).");
        }

        return detected;
    }

    private async Task<string> SaveOriginalFileAsync(
        IFormFile file,
        string fileName,
        string? subfolder,
        CancellationToken cancellationToken)
    {
        await using var originalStream = file.OpenReadStream();
        var originalStoragePath = pathResolver.BuildRelativePath(subfolder, _settings.OriginalsSubfolder, fileName);

        await storage.SaveAsync(
            originalStream,
            originalStoragePath,
            cancellationToken);

        return pathResolver.BuildPublicUrl(subfolder, _settings.OriginalsSubfolder, fileName);
    }

    // This method swallows an error thrown by thumbnailer because failing to create thumbnail doesn't violate business rule.
    // In case if requirements change, remove try-catch or propogate the erorr further, and delete the earlier created original file if needed.
    private async Task<string?> TrySaveThumbnailAsync(
        IFormFile file,
        string fileName,
        string extension,
        string? subfolder,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.AllowedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var thumbnailFormat = ImageHelper.GetThumbnailFormat(extension);
            await using var thumbSourceStream = file.OpenReadStream();
            await using var thumbStream = await thumbnailer.GenerateThumbnailAsync(thumbSourceStream, thumbnailFormat, cancellationToken);
            var thumbnailStoragePath = pathResolver.BuildRelativePath(subfolder, _settings.ThumbnailsSubfolder, fileName);

            await storage.SaveAsync(
                thumbStream,
                thumbnailStoragePath,
                cancellationToken);

            return pathResolver.BuildPublicUrl(subfolder, _settings.ThumbnailsSubfolder, fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "There was an error thumbnailing the image {FileName}. Error details: {Message}",
                file.FileName,
                ex.Message);

            return null;
        }
    }

    private Task DeleteOriginalAsync(
        string fileName,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
    {
        var originalPath = pathResolver.BuildRelativePath(subfolder, _settings.OriginalsSubfolder, fileName);

        return storage.DeleteAsync(originalPath, cancellationToken);
    }

    private Task TryDeleteThumbnailAsync(
        string fileName,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName);
        if (!_settings.AllowedImageExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Task.CompletedTask; // No thumbnail to delete
        }

        var thumbnailPath = pathResolver.BuildRelativePath(subfolder, _settings.ThumbnailsSubfolder, fileName);

        return storage.DeleteAsync(thumbnailPath, cancellationToken);
    }
}
