using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Exceptions;

public sealed class UnsupportedFileFormatExceptionTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetEmptyProperties()
    {
        // Act
        var exception = new UnsupportedFileFormatException();

        // Assert
        Assert.Equal(string.Empty, exception.Extension);
        Assert.Empty(exception.Allowed);
        Assert.NotNull(exception.Message);
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new UnsupportedFileFormatException("test message");

        // Assert
        Assert.Equal(string.Empty, exception.Extension);
        Assert.Empty(exception.Allowed);
        Assert.Equal("test message", exception.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var innerException = new Exception("inner exception");

        // Act
        var exception = new UnsupportedFileFormatException("test message", innerException);

        // Assert
        Assert.Equal(string.Empty, exception.Extension);
        Assert.Empty(exception.Allowed);
        Assert.Equal("test message", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_WithExtensionAndAllowed_ShouldSetProperties()
    {
        // Arrange
        var extension = ".exe";
        var allowed = new[] { ".jpg", ".png" };

        // Act
        var exception = new UnsupportedFileFormatException(extension, allowed);

        // Assert
        Assert.Equal(extension, exception.Extension);
        Assert.Equal(allowed, exception.Allowed);
        Assert.Equal($"Extension '{extension}' is not permitted. Allowed extensions: {string.Join(", ", allowed)}", exception.Message);
    }
}
