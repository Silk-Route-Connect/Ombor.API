using Ombor.Application.Helpers;
using Ombor.Application.Models;

namespace Ombor.Tests.Unit.Helpers;

public sealed class ImageHelperTests
{
    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData(" ", typeof(ArgumentException))]
    [InlineData("   ", typeof(ArgumentException))]
    public void GetThumbnailFormat_ShouldThrowException_WhenExtensionIsInvalid(string? extension, Type expectedExceptionType)
    {
        Assert.Throws(expectedExceptionType, () => ImageHelper.GetThumbnailFormat(extension!));
    }

    [Theory]
    [InlineData(".pNg", ThumbnailFormat.Png)]
    [InlineData(".JpG", ThumbnailFormat.Jpg)]
    [InlineData(".JPEG", ThumbnailFormat.Jpg)]
    [InlineData(".webp  ", ThumbnailFormat.Webp)]
    [InlineData("  .gif", ThumbnailFormat.Gif)]
    public void GetFormat_ShouldReturnCorrectFormat_WhenExtensionIsValid(string extension, ThumbnailFormat expectedFormat)
    {
        // Act
        var format = ImageHelper.GetThumbnailFormat(extension);
        // Assert
        Assert.Equal(expectedFormat, format);
    }

    [Theory]
    [InlineData(".pdf")]
    [InlineData(".zip")]
    [InlineData(".txt")]
    public void GetImageFormat_ShouldReturnNull_WhenInvalidImageName(string imageName)
    {
        // Act & Assert
        var exception = Assert.Throws<NotSupportedException>(
            () => ImageHelper.GetThumbnailFormat(imageName));

        Assert.Equal($"Unsupported image format: {imageName}", exception.Message);
    }
}
