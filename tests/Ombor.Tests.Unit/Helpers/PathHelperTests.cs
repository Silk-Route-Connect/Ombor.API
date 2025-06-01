using Ombor.Infrastructure.Helpers;

namespace Ombor.Tests.Unit.Helpers;

public sealed class PathHelpersTests
{
    public static readonly TheoryData<string, string[]> ExtractSegmentsValidData = new()
    {
        { "a/b/c", new[] { "a", "b", "c" } },
        { "  /foo//bar\\baz.txt ", new[] { "foo", "bar", "baz.txt" } },
        { "/   /hello/  world  ", new[] { "hello", "world" } },
        { "singleSegment", new[] { "singleSegment" } }
    };

    public static readonly TheoryData<string> ExtractSegmentsInvalidData = new()
    {
        "",
        "   ",
        "good/.. /bad",
        "foo/invalid|char/bar"
    };

    public static readonly TheoryData<string?[], string[]> BuildSegmentsValidData = new()
    {
        { new[] { "foo", "bar", "baz" }, new[] { "foo", "bar", "baz" } },
        { new[] { "/sub/", "/sec/", "/file/ " }, new[] { "sub", "sec", "file" } },
        { new[] { " a ", " b ", " c " }, new[] { "a", "b", "c" } },
        { new[] { null, "keep", "  drop  ", "" }, new[] { "keep", "drop" } }
    };

    public static readonly TheoryData<string[]> BuildSegmentsInvalidData =
    [
        ["ok", "inva|id", "fine"],
        ["..", "good", "alsoGood"]
    ];

    public static readonly TheoryData<string, string> BuildRelativePath_Data = new()
    {
        { "a|b|c", "/a/b/c" },
        { "", "/" },
        { "single", "/single" }
    };

    [Fact]
    public void PathHelpers_ValidateSegments_ShouldThrowArgumentNullException_WhenSegmentsIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PathHelpers.ValidateSegments(null!));
    }

    [Theory]
    [InlineData("alpha")]
    [InlineData("one|two|three")]
    public void PathHelpers_ValidateSegments_ShouldNotThrow_WhenAllSegmentsValid(string joinedSegments)
    {
        // Arrange
        var segments = joinedSegments.Split('|');

        // Act & Assert
        PathHelpers.ValidateSegments(segments);
    }

    [Theory]
    [InlineData("..")]
    [InlineData("inva|id")]
    [InlineData("ques?tion")]
    public void PathHelpers_ValidateSegments_ShouldThrowArgumentException_WhenSegmentIsInvalid(string badSegment)
    {
        // Arrange
        var segments = new[] { "ok", badSegment, "fine" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PathHelpers.ValidateSegments(segments));

        Assert.Contains($"Invalid path segment: {badSegment}", exception.Message);
        Assert.Equal("segments", exception.ParamName);
    }

    [Fact]
    public void PathHelpers_ExtractSegments_ShouldThrowArgumentNullException_WhenPathIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PathHelpers.ExtractSegments(null!));
    }

    [Theory]
    [MemberData(nameof(ExtractSegmentsInvalidData))]
    public void PathHelpers_ExtractSegments_ShouldThrowArgumentException_WhenPathIsEmptyWhitespaceOrContainsInvalid(string path)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            PathHelpers.ExtractSegments(path));

        Assert.Equal("path", exception.ParamName);
    }

    [Theory]
    [MemberData(nameof(ExtractSegmentsValidData))]
    public void PathHelpers_ExtractSegments_ShouldSplitAndTrim_WhenPathIsValid(string inputPath, string[] expected)
    {
        // Act
        var actual = PathHelpers.ExtractSegments(inputPath);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void PathHelpers_BuildSegments_ShouldThrowArgumentNullException_WhenInputsArrayIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            PathHelpers.BuildSegments(null!));
    }

    [Theory]
    [InlineData(null, "   ")]
    [InlineData("", "  ")]
    public void PathHelpers_BuildSegments_ShouldReturnEmpty_WhenAllInputsNullOrWhitespace(string a, string b)
    {
        // Act
        var actual = PathHelpers.BuildSegments(a, b);

        // Assert
        Assert.Empty(actual);
    }

    [Theory]
    [MemberData(nameof(BuildSegmentsValidData))]
    public void PathHelpers_BuildSegments_ShouldTrimAndFilter_WhenInputsValid(string[] inputs, string[] expected)
    {
        // Act
        var actual = PathHelpers.BuildSegments(inputs);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(BuildSegmentsInvalidData))]
    public void PathHelpers_BuildSegments_ShouldThrowArgumentException_WhenAnyInputInvalid(string[] inputs)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            PathHelpers.BuildSegments(inputs));
    }

    [Theory]
    [MemberData(nameof(BuildRelativePath_Data))]
    public void PathHelpers_BuildRelativePath_ShouldJoinWithSlash_WhenSegmentsProvided(string joined, string expected)
    {
        // Arrange
        var segments = string.IsNullOrEmpty(joined)
            ? []
            : joined.Split('|');

        // Act
        var actual = PathHelpers.BuildRelativePath(segments);

        // Assert
        Assert.Equal(expected, actual);
    }
}