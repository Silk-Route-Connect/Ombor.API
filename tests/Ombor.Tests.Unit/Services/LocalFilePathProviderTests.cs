using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Infrastructure.Services;

namespace Ombor.Tests.Unit.Services;

public sealed class LocalFilePathProviderTests
{
    private readonly LocalFilePathProvider _provider;

    private static readonly string[] _expectedPathsWithoutSubfolder = ["base", "section", "file.txt"];
    private static readonly string[] _expectedPathsWithSubfolder = ["base", "sub", "section", "file.txt"];

    public static TheoryData<string?, string, string, string[]> ValidPathData => new()
    {
        // no subfolder
        { null, "section", "file.txt", _expectedPathsWithoutSubfolder },
        { "", "section", "file.txt", _expectedPathsWithoutSubfolder },

        // with subfolder, trimming slashes
        { "sub", "section", "file.txt", _expectedPathsWithSubfolder },
        { "/sub/", "/section/", "/file.txt", _expectedPathsWithSubfolder },

        // whitespace around segments
        { "  sub  ", " section  ", "  file.txt  ", _expectedPathsWithSubfolder }
    };

    public LocalFilePathProviderTests()
    {
        var settings = new FileSettings
        {
            BasePath = "base",
            MaxBytes = 1024,
            AllowedImageExtensions = [],
            AllowedFileExtensions = [],
            ThumbnailWidth = default,
            ThumbnailHeight = default,
            OriginalsSubfolder = "originals",
            ThumbnailsSubfolder = "thumbnails"
        };
        var options = Options.Create(settings);

        _provider = new LocalFilePathProvider(options);
    }

    [Theory]
    [MemberData(nameof(ValidPathData))]
    public void BuildRelativePath_ShouldCombinedPath_WhenInputsAreValid(
        string subfolder,
        string section,
        string fileName,
        string[] expectedSegments)
    {
        // Act
        string actualPath = _provider.BuildRelativePath(subfolder, section, fileName);

        // Assert
        string expectedPath = Path.Combine(expectedSegments);
        Assert.Equal(expectedPath, actualPath);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("inva|id")]
    [InlineData("null\0char")]
    public void BuildRelativePath_ShouldThrowArgumentException_WhenSubfolderIsInvalid(string invalidSubfolder)
    {
        Assert.Throws<ArgumentException>(() =>
            _provider.BuildRelativePath(invalidSubfolder, "section", "file.txt"));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("sec?ion")]
    [InlineData("sec/ion")]
    public void BuildRelativePath_ShouldThrowArgumentException_WhenSectionIsInvalid(string invalidSection)
    {
        Assert.Throws<ArgumentException>(() =>
            _provider.BuildRelativePath("sub", invalidSection, "file.txt"));
    }

    [Theory]
    [InlineData("..")]
    [InlineData("fi<le>.txt")]
    [InlineData("nul\0l.txt")]
    public void BuildRelativePath_ShouldThrowArgumentException_WhenFileNameIsInvalid(string invalidFileName)
    {
        Assert.Throws<ArgumentException>(() =>
            _provider.BuildRelativePath("sub", "section", invalidFileName));
    }
}