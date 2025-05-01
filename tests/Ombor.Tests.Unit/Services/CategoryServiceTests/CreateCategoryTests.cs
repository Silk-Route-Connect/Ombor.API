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

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedCategory_WhenRequestIsValid()
    {
        // Arrange
        var request = CategoryRequestFactory.GenerateValidCreateRequest();
        Category expected = null!;

        _mockContext.Setup(c => c.Categories.Add(It.Is<Category>(categoryToAdd => categoryToAdd.IsEquivalent(request))))
            .Callback<Category>(captured => expected = captured);

        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => expected.Id = 99);

        // Act
        var actual = await _service.CreateAsync(request);

        // Assert
        CategoryAssertionHelper.AssertEquivalent(expected, actual);

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories.Add(expected), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
