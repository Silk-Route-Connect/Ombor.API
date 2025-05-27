using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Extensions;
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

        var originalUrl = await SaveOriginalFile(
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

    private async Task<string> SaveOriginalFile(
        IFormFile file,
        string fileName,
        string? subfolder,
        CancellationToken cancellationToken)
    {
        await using var originalStream = file.OpenReadStream();
        var originalStoragePath = pathResolver.BuildRelativePath(subfolder, _settings.OriginalsSubfolder, fileName);

        return await storage.UploadAsync(
            originalStream,
            originalStoragePath,
            cancellationToken);
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
            var thumbnailFormat = GetFormat(extension);
            await using var thumbSourceStream = file.OpenReadStream();
            await using var thumbStream = await thumbnailer.GenerateThumbnailAsync(thumbSourceStream, thumbnailFormat, cancellationToken);

            var thumbnailStoragePath = pathResolver.BuildRelativePath(subfolder, _settings.ThumbnailsSubfolder, fileName);

            return await storage.UploadAsync(
                thumbStream,
                thumbnailStoragePath,
                cancellationToken);
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

    private static ThumbnailFormat GetFormat(string extension) =>
        extension switch
        {
            ".png" => ThumbnailFormat.Png,
            ".jpg" => ThumbnailFormat.Jpg,
            ".jpeg" => ThumbnailFormat.Jpeg,
            ".webp" => ThumbnailFormat.Webp,
            ".gif" => ThumbnailFormat.Gif,
            _ => throw new ArgumentOutOfRangeException($"Thumbnailing of {extension} format is not supported."),
        };
}
