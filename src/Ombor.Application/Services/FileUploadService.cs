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

internal sealed class FileUploadService(
    IFileStorage storage,
    IImageThumbnailer thumbnailer,
    IFilePathProvider pathResolver,
    IOptions<FileSettings> options,
    ILogger<FileUploadService> logger) : IFileUploadService
{
    private readonly FileSettings _settings = options.Value;

    public async Task<FileUploadResult> UploadAsync(
        IFormFile file,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
    {
        ValidateOrThrow(file);

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
            ThumbnailUrl: thumbnailUrl);
    }

    public Task<FileUploadResult[]> UploadAsync(
        IEnumerable<IFormFile> files,
        string? subfolder = null,
        CancellationToken cancellationToken = default)
    {
        var tasks = files.Select(f => UploadAsync(f, subfolder, cancellationToken));

        return Task.WhenAll(tasks);
    }

    private void ValidateOrThrow(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is required.", nameof(file));
        }

        if (string.IsNullOrWhiteSpace(file.FileName))
        {
            throw new ArgumentException("File name is required.", nameof(file));
        }

        if (file.Length > _settings.MaxBytes)
        {
            throw new FileTooLargeException(file.Length, _settings.MaxBytes);
        }

        var extension = file.GetNormalizedFileExtension();
        if (!_settings.AllowedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new UnsupportedFileFormatException(extension, _settings.AllowedFileExtensions);
        }
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
            var thumbnailFormat = ImageHelpers.GetFormat(extension);
            await using var thumbSourceStream = file.OpenReadStream();
            await using var thumbStream = await thumbnailer.GenerateThumbnailAsync(thumbSourceStream, thumbnailFormat, cancellationToken);
            var thumbnailStoragePath = pathResolver.BuildRelativePath(subfolder, _settings.ThumbnailsSubfolder, fileName);

            await storage.SaveAsync(
                thumbStream,
                thumbnailStoragePath,
                cancellationToken);

            return pathResolver.BuildPublicUrl(subfolder, _settings.OriginalsSubfolder, fileName);
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
}
