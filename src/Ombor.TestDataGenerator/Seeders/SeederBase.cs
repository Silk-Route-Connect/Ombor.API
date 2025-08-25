using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Ombor.Application.Configurations;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces.File;
using Ombor.TestDataGenerator.Generators;

namespace Ombor.TestDataGenerator.Seeders;

internal abstract class SeederBase(
    FileSettings fileSettings,
    IWebHostEnvironment env,
    IImageThumbnailer thumbnailer)
{
    private const string imagesNamespace = "Ombor.TestDataGenerator.Resources.Images.";
    private readonly Assembly currentAssembly = typeof(ProductGenerator).Assembly;

    protected static readonly Random _random = new();
    protected readonly FileSettings fileSettings = fileSettings;

    protected Task<Dictionary<string, string>> EnsureImagesCopiedAsync()
    {
        var originalsDirectory = Path.Combine(
            env.WebRootPath,
            fileSettings.BasePath,
            fileSettings.ProductUploadsSection,
            fileSettings.OriginalsSubfolder);
        var thumbsDirectory = Path.Combine(
            env.WebRootPath,
            fileSettings.BasePath,
            fileSettings.ProductUploadsSection,
            fileSettings.ThumbnailsSubfolder);

        if (Directory.Exists(originalsDirectory))
        {
            Directory.Delete(originalsDirectory, true);
        }

        if (Directory.Exists(thumbsDirectory))
        {
            Directory.Delete(thumbsDirectory, true);
        }

        Directory.CreateDirectory(originalsDirectory);
        Directory.CreateDirectory(thumbsDirectory);

        return ExtractAndSaveImagesAsync(originalsDirectory, thumbsDirectory);
    }

    private async Task<Dictionary<string, string>> ExtractAndSaveImagesAsync(string originalsDir, string thumbsDir)
    {
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var resourceNames = currentAssembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(imagesNamespace, StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            var originalFileName = resourceName[imagesNamespace.Length..];
            var extension = Path.GetExtension(originalFileName);
            var storageFileName = $"{Guid.NewGuid():N}{extension}";

            nameMap[storageFileName] = originalFileName;

            // copy original
            await using var originalImageStream = currentAssembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException(resourceName);
            await using var originalImageFileStream = File.Create(Path.Combine(originalsDir, storageFileName));
            await originalImageStream.CopyToAsync(originalImageFileStream);

            // generate & save thumbnail
            originalImageStream.Position = 0;
            var format = ImageHelper.GetThumbnailFormat(extension);
            await using var thumbnailStream = await thumbnailer.GenerateThumbnailAsync(originalImageFileStream, format);
            await using var thumbnailImageFileStream = File.Create(Path.Combine(thumbsDir, storageFileName));
            await thumbnailStream.CopyToAsync(thumbnailImageFileStream);
        }

        return nameMap;
    }
}
