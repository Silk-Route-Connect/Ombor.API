using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Models;
using Ombor.Infrastructure.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ombor.Tests.Unit.Services;

public sealed class ImageSharpThumbnailerTests
{
    private const int DefaultThumbnailSize = 50;
    public static TheoryData<int, int, ThumbnailFormat, IImageFormat> ThumbnailData => new()
    {
        { 200, 100, ThumbnailFormat.Png, PngFormat.Instance },
        { 200, 200, ThumbnailFormat.Jpg, JpegFormat.Instance },
        { 300, 200, ThumbnailFormat.Webp, WebpFormat.Instance },
        { 400, 100, ThumbnailFormat.Gif, GifFormat.Instance }
    };

    [Theory]
    [MemberData(nameof(ThumbnailData))]
    public async Task GenerateThumbnailAsync_ShouldProduceSmallerImage_WhenFormatIsSupported(
        int originalWidth,
        int originalHeight,
        ThumbnailFormat format,
        IImageFormat expectedFormat)
    {
        // Arrange
        var service = CreateService(DefaultThumbnailSize, DefaultThumbnailSize);
        await using var sourceStream = CreateTestImageStream(originalWidth, originalHeight);

        // Act
        await using var thumbStream = await service.GenerateThumbnailAsync(
            sourceStream,
            format,
            default);

        // Assert
        Assert.NotNull(thumbStream);
        Assert.True(thumbStream.Length > 0);

        thumbStream.Position = 0;
        IImageFormat actualFormat = await Image.DetectFormatAsync(thumbStream);
        Assert.Same(expectedFormat, actualFormat);

        thumbStream.Position = 0;
        using var resultImage = await Image.LoadAsync(thumbStream);
        Assert.InRange(resultImage.Width, 1, DefaultThumbnailSize);
        Assert.InRange(resultImage.Height, 1, DefaultThumbnailSize);
    }

    [Fact]
    public async Task GenerateThumbnailAsync_ShouldThrowArgumentOutOfRangeException_WhenFormatUnsupported()
    {
        // Arrange
        var service = CreateService(DefaultThumbnailSize);
        await using var sourceStream = CreateTestImageStream(100, 100);
        const ThumbnailFormat unsupportedFormat = (ThumbnailFormat)(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
        {
            await service.GenerateThumbnailAsync(
                sourceStream,
                unsupportedFormat,
                default);
        });
    }

    private static MemoryStream CreateTestImageStream(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Rgba32.ParseHex("#d93057"));

        var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        ms.Position = 0;

        return ms;
    }

    private static ImageSharpThumbnailer CreateService(int width)
        => CreateService(width, width);

    private static ImageSharpThumbnailer CreateService(int thumbWidth, int thumbHeight)
    {
        var settings = new FileSettings
        {
            BasePath = "uploads",
            MaxBytes = long.MaxValue,
            AllowedImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"],
            AllowedFileExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"],
            ThumbnailWidth = thumbWidth,
            ThumbnailHeight = thumbHeight,
            OriginalsSubfolder = "orig",
            ThumbnailsSubfolder = "thumb",
            ProductUploadsSection = "products",
            PublicUrlPrefix = "images",
            PaymentAttachmentsSection = "payments"
        };

        var options = Options.Create(settings);

        return new ImageSharpThumbnailer(options);
    }
}
