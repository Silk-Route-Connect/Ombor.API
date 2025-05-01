using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class UpdateCategoryTests : CategoryTestsBase
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = CategoryRequestFactory.GenerateInvalidUpdateRequest(CategoryId);

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = CategoryRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Category>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedCategory_WhenRequestIsValid()
    {
        // Arrange
        var categoryToUpdate = _builder.CategoryBuilder
            .WithId(CategoryId)
            .BuildAndPopulate();
        var request = CategoryRequestFactory.GenerateValidUpdateRequest(categoryToUpdate.Id);

        SetupCategories([.. _defaultCategories, categoryToUpdate]);

        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        CategoryAssertionHelper.AssertEquivalent(request, response);
        CategoryAssertionHelper.AssertEquivalent(request, categoryToUpdate);

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(c => c.Categories, Times.Once);

        VerifyNoOtherCalls();
    }
}
