using Moq;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Exceptions;

public sealed class EntityNotFoundExceptionTests
{
    [Fact]
    public void Base_DefaultConstructor_ShouldSetEmptyProperties()
    {
        // Act
        var mockException = new Mock<EntityNotFoundException>() { CallBase = true };
        var ex = mockException.Object;

        // Assert
        Assert.Equal(string.Empty, ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(string.Empty, ex.Id);
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void Base_Constructor_WithMessage_ShouldSetMessage()
    {
        // Act
        var mockException = new Mock<EntityNotFoundException>("test message") { CallBase = true };
        var ex = mockException.Object;

        // Assert
        Assert.Equal("test message", ex.Message);
        Assert.Equal(string.Empty, ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(string.Empty, ex.Id);
    }

    [Fact]
    public void Base_Constructor_WithMessageAndInnerEx_ShouldSetProperties()
    {
        // Arrange
        var innerEx = new Exception("inner exception");

        // Act
        var mockException = new Mock<EntityNotFoundException>("test message", innerEx) { CallBase = true };
        var ex = mockException.Object;

        // Assert
        Assert.Equal("test message", ex.Message);
        Assert.Equal(innerEx, ex.InnerException);
        Assert.Equal(string.Empty, ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(string.Empty, ex.Id);
    }

    [Fact]
    public void Base_Constructor_WithTypeAndId_ShouldSetProperties()
    {
        // Act
        var mockException = new Mock<EntityNotFoundException>(typeof(Category), 42) { CallBase = true };
        var ex = mockException.Object;

        // Assert
        Assert.Equal("Category with ID 42 was not found.", ex.Message);
        Assert.Equal(nameof(Category), ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(42, ex.Id);
    }

    [Fact]
    public void Generic_DefaultConstructor_ShouldSetEmptyProperties()
    {
        // Act
        var ex = new EntityNotFoundException<Category>();

        // Assert
        Assert.Equal(string.Empty, ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(string.Empty, ex.Id);
        Assert.NotNull(ex.Message);
    }

    [Fact]
    public void Generic_Constructor_WithMessage_ShouldSetMessage()
    {
        // Act
        var ex = new EntityNotFoundException<Category>("test message");

        // Assert
        Assert.Equal("test message", ex.Message);
        Assert.Equal(string.Empty, ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(string.Empty, ex.Id);
    }

    [Fact]
    public void Generic_Constructor_WithMessageAndInnerEx_ShouldSetProperties()
    {
        // Arrange
        var innerEx = new Exception("test inner exception");

        // Act
        var ex = new EntityNotFoundException<Category>("test message", innerEx);

        // Assert
        Assert.Equal("test message", ex.Message);
        Assert.Equal(innerEx, ex.InnerException);
        Assert.Equal(string.Empty, ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(string.Empty, ex.Id);
    }

    [Fact]
    public void Generic_Constructor_WithId_ShouldSetProperties()
    {
        // Act
        var ex = new EntityNotFoundException<Category>(99);

        // Assert
        Assert.Equal("Category with ID 99 was not found.", ex.Message);
        Assert.Equal(nameof(Category), ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(99, ex.Id);
    }

    [Fact]
    public void Generic_Constructor_WithTypeAndId_ShouldSetProperties()
    {
        // Act
        var ex = new EntityNotFoundException<Category>(typeof(Category), 1001);

        // Assert
        Assert.Equal("Category with ID 1001 was not found.", ex.Message);
        Assert.Equal(nameof(Category), ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal(1001, ex.Id);
    }

    [Fact]
    public void Generic_Constructor_WithDifferentTypeAndId_ShouldSetProperties()
    {
        // Act
        var ex = new EntityNotFoundException<Category>(typeof(Product), "abc");

        // Assert
        Assert.Equal("Product with ID abc was not found.", ex.Message);
        Assert.Equal(nameof(Product), ex.EntityType);
        Assert.Equal(nameof(EntityNotFoundException), ex.ExceptionType);
        Assert.Equal("abc", ex.Id);
    }
}
