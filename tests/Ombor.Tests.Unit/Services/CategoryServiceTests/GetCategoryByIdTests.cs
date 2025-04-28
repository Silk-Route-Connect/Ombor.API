using System.ComponentModel.DataAnnotations;
using AutoFixture;
using Moq;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class GetCategoryByIdTests : CategoryTestsBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = _fixture.Create<GetCategoryByIdRequest>();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = new GetCategoryByIdRequest(999);
        Category? expected = null;

        _mockValidator.Setup(v => v.ValidateAndThrow(request));
        _mockContext.Setup(c => c.Categories.FindAsync(request.Id))
            .ReturnsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Category>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenCategoryIsFound()
    {
        // Arrange
        var expected = new Category { Id = 99, Name = "Test Category Name", Description = "Test Category Description" };
        var request = new GetCategoryByIdRequest(expected.Id);

        _mockValidator.Setup(v => v.ValidateAndThrow(request));
        _mockContext.Setup(c => c.Categories.FindAsync(request.Id))
            .ReturnsAsync(expected);

        // Act
        var response = await _service.GetByIdAsync(request);

        // Assert
        Assert.Equal(expected.Id, response.Id);
        Assert.Equal(expected.Name, response.Name);
        Assert.Equal(expected.Description, response.Description);

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Once);
    }
}
