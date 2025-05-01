using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class GetCategoryByIdTests : CategoryTestsBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new GetCategoryByIdRequest(CategoryId);

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = new GetCategoryByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Category>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<GetCategoryByIdRequest>()), Times.Once);
        _mockContext.Verify(c => c.Categories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenCategoryIsFound()
    {
        // Arrange
        var expected = _builder.CategoryBuilder
            .WithId(CategoryId)
            .BuildAndPopulate();
        var request = new GetCategoryByIdRequest(expected.Id);

        SetupCategories([.. _defaultCategories, expected]);

        // Act
        var actual = await _service.GetByIdAsync(request);

        // Assert
        CategoryAssertionHelper.AssertEquivalent(expected, actual);

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<GetCategoryByIdRequest>()), Times.Once);
        _mockContext.Verify(c => c.Categories, Times.Once);

        VerifyNoOtherCalls();
    }
}
