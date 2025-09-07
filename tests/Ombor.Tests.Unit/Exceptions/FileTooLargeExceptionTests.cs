using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Exceptions;

public sealed class FileTooLargeExceptionTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetEmptyProperties()
    {
        // Act
        var ex = new FileTooLargeException();

        // Assert
        Assert.Null(ex.Source);
        Assert.Null(ex.TargetSite);
        Assert.Null(ex.InnerException);
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Act
        var ex = new FileTooLargeException("test message");

        // Assert
        Assert.Null(ex.Source);
        Assert.Null(ex.TargetSite);
        Assert.Null(ex.InnerException);
        Assert.Equal("test message", ex.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var innerException = new Exception("test inner exception");

        // Act
        var ex = new FileTooLargeException("test message", innerException);

        // Assert
        Assert.Null(ex.Source);
        Assert.Null(ex.TargetSite);
        Assert.Equal(innerException, ex.InnerException);
        Assert.Equal("test message", ex.Message);
    }

    [Fact]
    public void Constructor_WithActualAndMax_ShouldSetMessage()
    {
        // Act
        var ex = new FileTooLargeException(2048, 1024);

        // Assert
        Assert.Null(ex.Source);
        Assert.Null(ex.TargetSite);
        Assert.Null(ex.InnerException);
        Assert.Equal("File size 2048 exceeds max 1024 bytes.", ex.Message);
    }
}
