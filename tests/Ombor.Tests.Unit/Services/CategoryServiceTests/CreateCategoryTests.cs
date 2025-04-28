using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class CreateCategoryTests : CategoryTestsBase
{
    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new CreateCategoryRequest("New Category", "Category Description");

        _mockValidator.Setup(v => v.ValidateAndThrow(request))
            .Throws(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(v => v.ValidateAndThrow(request), Times.Once);
        _mockContext.Verify(c => c.Categories.Add(It.IsAny<Category>()), Times.Never);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedCategory_WhenRequestIsValid()
    {
        // Arrange
        var request = new CreateCategoryRequest("New Category Name", "New Category Description");
        Category captured = null!;

        _mockContext.Setup(c => c.Categories.Add(It.Is<Category>(e => e.Name == request.Name && e.Description == request.Description)))
            .Callback<Category>(e => captured = e);

        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => captured.Id = 99);

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.Equal(99, response.Id);
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);

        _mockValidator.Verify(v => v.ValidateAndThrow(It.IsAny<CreateCategoryRequest>()), Times.Once);
        _mockContext.Verify(c => c.Categories.Add(It.IsAny<Category>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
