using System.ComponentModel.DataAnnotations;
using AutoFixture;
using Moq;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class UpdateCategoryTests : CategoryTestsBase
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = _fixture.Create<UpdateCategoryRequest>();

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<UpdateCategoryRequest>()), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = _fixture.Create<UpdateCategoryRequest>();
        Category? expected = null;

        _mockContext.Setup(c => c.Categories.FindAsync(request.Id))
            .ReturnsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Category>>(
               () => _service.UpdateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<UpdateCategoryRequest>()), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedCategory_WhenRequestIsValid()
    {
        // Arrange
        var categoryToUpdate = new Category { Id = 99, Name = "Old Category Name", Description = "Old Category Descirption" };
        var request = new UpdateCategoryRequest(99, "Updated Category Name", "Updated Category Description");

        _mockContext.Setup(c => c.Categories.FindAsync(request.Id))
            .ReturnsAsync(categoryToUpdate);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories.FindAsync(It.IsAny<int>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
