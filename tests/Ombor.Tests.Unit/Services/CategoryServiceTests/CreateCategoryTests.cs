using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class CreateCategoryTests : CategoryTestsBase
{
    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = CategoryRequestFactory.GenerateInvalidCreateRequest();

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedCategory_WhenRequestIsValid()
    {
        // Arrange
        var request = CategoryRequestFactory.GenerateValidCreateRequest();
        Category addedCategory = null!;

        _mockContext.Setup(mock => mock.Categories.Add(It.Is<Category>(category => category.IsEquivalent(request))))
            .Callback<Category>(captured => addedCategory = captured);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => addedCategory.Id = 99);

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        CategoryAssertionHelper.AssertEquivalent(request, response);
        CategoryAssertionHelper.AssertEquivalent(request, addedCategory);
        CategoryAssertionHelper.AssertEquivalent(addedCategory, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Categories.Add(addedCategory), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
