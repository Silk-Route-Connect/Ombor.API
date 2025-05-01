using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class DeleteCategoryTests : CategoryTestsBase
{
    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new DeleteCategoryRequest(CategoryId);

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = new DeleteCategoryRequest(NonExistentEntityId);

        _mockValidator.Setup(v => v.ValidateAndThrow(request));

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Category>>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveCategory_WhenCategoryExists()
    {
        // Arrange
        var categoryToDelete = _builder.CategoryBuilder
            .WithId(CategoryId)
            .BuildAndPopulate();
        var request = new DeleteCategoryRequest(CategoryId);

        _mockValidator.Setup(v => v.ValidateAndThrow(request));

        var mockSet = SetupCategories([.. _defaultCategories, categoryToDelete]);
        mockSet.Setup(c => c.Remove(categoryToDelete));

        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(request);

        // Assert
        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        mockSet.Verify(c => c.Remove(categoryToDelete), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(c => c.Categories, Times.Exactly(2));

        VerifyNoOtherCalls();
    }
}
