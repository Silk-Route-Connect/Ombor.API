using Moq;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Exceptions;

public sealed class InvalidFileExceptionsTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetEmptyProperties()
    {
        // Act
        var exception = new Mock<InvalidFileException>() { CallBase = true };
        var ex = exception.Object;

        // Assert
        Assert.NotNull(ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Constructor_WithMessage_ShouldSetMessage()
    {
        // Act
        var exception = new Mock<InvalidFileException>("test message") { CallBase = true };
        var ex = exception.Object;

        // Assert
        Assert.Equal("test message", ex.Message);
        Assert.Null(ex.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetProperties()
    {
        // Arrange
        var innerException = new Exception("inner exception");

        // Act
        var exception = new Mock<InvalidFileException>("test message", innerException) { CallBase = true };
        var ex = exception.Object;

        // Assert
        Assert.Equal("test message", ex.Message);
        Assert.Equal(innerException, ex.InnerException);
    }
}
