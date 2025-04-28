using System.ComponentModel.DataAnnotations;
using AutoFixture;
using Moq;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.TestDataGenerator.Generators.Entities;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class DeleteCategoryTests : CategoryTestsBase
{
    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = _fixture.Create<DeleteCategoryRequest>();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        _mockContext.Setup(c => c.Categories.FindAsync(It.IsAny<int>()));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<DeleteCategoryRequest>()), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Never);
        _mockContext.Verify(c => c.Categories.Remove(It.IsAny<Category>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        Category? expected = null;
        var request = _fixture.Create<DeleteCategoryRequest>();

        _mockContext.Setup(c => c.Categories.FindAsync(request.Id))
            .ReturnsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Category>>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Once);
        _mockContext.Verify(c => c.Categories.Remove(It.IsAny<Category>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCategory_WhenCategoryExists()
    {
        // Arrange
        var categoryToDelete = CategoryGenerator.Generate();
        var request = _fixture.Create<DeleteCategoryRequest>();

        _mockContext.Setup(c => c.Categories.FindAsync(request.Id))
            .ReturnsAsync(categoryToDelete);
        _mockContext.Setup(c => c.Categories.Remove(categoryToDelete));

        // Act
        await _service.DeleteAsync(request);

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Once);
        _mockContext.Verify(c => c.Categories.Remove(It.IsAny<Category>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
