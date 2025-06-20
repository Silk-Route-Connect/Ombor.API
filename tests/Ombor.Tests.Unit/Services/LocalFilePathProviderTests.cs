using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Infrastructure.Services;

namespace Ombor.Tests.Unit.Services;

public sealed class LocalFilePathProviderTests
{
    private readonly LocalFilePathProvider _provider;
    private static readonly FileSettings _settings = new()
    {
        BasePath = "base",
        PublicUrlPrefix = "public",
        MaxBytes = 1024,
        AllowedImageExtensions = [],
        AllowedFileExtensions = [],
        ThumbnailWidth = 10,
        ThumbnailHeight = 10,
        OriginalsSubfolder = "originals",
        ThumbnailsSubfolder = "thumbnails",
        ProductUploadsSection = "products",
        PaymentAttachmentsSection = "payments"
    };

    public LocalFilePathProviderTests()
    {
        var options = Options.Create(_settings);

        _provider = new LocalFilePathProvider(options);
    }

    public static readonly TheoryData<string?, string, string, string[]> BuildRelativePathValidData = new()
    {
        { null, "section", "file.txt", new[] { "base", "section", "file.txt" } },
        { "", "section", "file.txt", new[] { "base", "section", "file.txt" } },
        { "sub", "section", "file.txt", new[] { "base", "sub", "section", "file.txt" } },
        { "/sub/", "/sec/", "/file.txt", new[] { "base", "sub", "sec", "file.txt" } },
        { "  sub  ", " section  ", "  file.txt  ", new[] { "base", "sub", "section", "file.txt" } }
    };

    public static readonly TheoryData<string?, string, string, string[]> BuildPublicUrl_ValidData = new()
    {
        { null, "section", "file.txt", new[] { "public", "section", "file.txt" } },
        { "", "section", "file.txt", new[] { "public", "section", "file.txt" } },
        { "sub", "section", "file.txt", new[] { "public", "sub", "section", "file.txt" } },
        { "/sub/", "/sec/", "/file.txt", new[] { "public", "sub", "sec", "file.txt" } },
        { "  sub  ", " section  ", "  file.txt  ", new[] { "public", "sub", "section", "file.txt" } }
    };

    [Theory]
    [MemberData(nameof(BuildRelativePathValidData))]
    public void BuildRelativePath_ShouldCombineCorrectly_WhenInputsValid(
        string subfolder,
        string section,
        string fileName,
        string[] expectedSegments)
    {
        // Act
        var actual = _provider.BuildRelativePath(subfolder, section, fileName);

        // Assert
        var expected = Path.Combine(expectedSegments);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("..", "section", "file.txt")]
    [InlineData("sub", "..", "file.txt")]
    [InlineData("sub", "section", "..")]
    [InlineData("inva|id", "section", "file.txt")]
    public void BuildRelativePath_ShouldThrowArgumentException_WhenAnySegmentInvalid(
        string subfolder,
        string section,
        string fileName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _provider.BuildRelativePath(subfolder, section, fileName));
    }

    [Theory]
    [MemberData(nameof(BuildPublicUrl_ValidData))]
    public void BuildPublicUrl_ShouldJoinWithSlash_WhenInputsValid(
        string subfolder,
        string section,
        string fileName,
        string[] expectedSegments)
    {
        // Act
        var actual = _provider.BuildPublicUrl(subfolder, section, fileName);

        // Assert
        var expected = string.Join('/', expectedSegments);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("..", "section", "file.txt")]
    [InlineData("sub", "..", "file.txt")]
    [InlineData("sub", "section", "..")]
    [InlineData("inva|id", "section", "file.txt")]
    public void BuildPublicUrl_ShouldThrowArgumentException_WhenAnySegmentInvalid(
        string subfolder,
        string section,
        string fileName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _provider.BuildPublicUrl(subfolder, section, fileName));
    }
}