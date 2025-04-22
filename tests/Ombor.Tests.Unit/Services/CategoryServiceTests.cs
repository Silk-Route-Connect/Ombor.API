using AutoFixture;
using Castle.Core.Internal;
using FluentValidation;
using MockQueryable.Moq;
using Moq;
using Ombor.Application.Services;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.TestDataGenerator.Generators.Entities;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Services;

public sealed class CategoryServiceTests : ServiceTestsBase
{
    private readonly CategoryService _service;
    private readonly Category[] _defaultCategories;

    public CategoryServiceTests()
    {
        _defaultCategories = CategoryGenerator.Generate(5).ToArray();
        SetupCategories(_defaultCategories);

        _service = new CategoryService(_mockContext.Object, _mockValidator.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetCategoriesRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoCategories()
    {
        // Arrange
        var request = new GetCategoriesRequest(string.Empty);
        SetupCategories([]);

        // Act
        var result = await _service.GetAsync(request);

        // Assert
        Assert.Empty(result);

        _mockContext.Verify(c => c.Categories, Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnAll_WhenNoSearchTermProvided()
    {
        // Arrange
        var request = new GetCategoriesRequest(string.Empty);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(_defaultCategories.Length, response.Length);

        _mockContext.Verify(c => c.Categories, Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnMatchingCategories_WhenSearchTermIsProvided()
    {
        // Arrange
        var searchTerm = "SearchMatch";
        var request = new GetCategoriesRequest(searchTerm);
        var matchingCategories = new Category[]
        {
                new() { Name = searchTerm, Description = "Desc" }, // Matching name
                new() { Name = "Name",  Description = searchTerm } // Matching description
        };
        SetupCategories([.. _defaultCategories, .. matchingCategories]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(matchingCategories.Length, response.Length);
        Assert.All(response, actual =>
        {
            var expected = matchingCategories.Find(x => x.Id == actual.Id);

            Assert.NotNull(expected);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Description, actual.Description);
        });

        _mockContext.Verify(c => c.Categories, Times.Once);
    }

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

        _mockContext.Verify(
            c => c.Categories.FindAsync(It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = new GetCategoryByIdRequest(999);

        _mockValidator.Setup(v => v.ValidateAndThrow(request));
        _mockContext.Setup(c => c.Categories.FindAsync(request.Id));

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Category>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(
            v => v.ValidateAndThrow(request),
            Times.Once);
        _mockContext.Verify(
            c => c.Categories.FindAsync(It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenCategoryIsFound()
    {
        // Arrange
        var expected = _defaultCategories.PickRandom();
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

        _mockValidator.Verify(
            v => v.ValidateAndThrow(request),
            Times.Once);
        _mockContext.Verify(
            c => c.Categories.FindAsync(It.IsAny<int>()),
            Times.Once);
    }

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

        _mockValidator.Verify(
            v => v.ValidateAndThrow(request),
            Times.Once);
        _mockContext.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedCategory_WhenRequestIsValid()
    {
        // Arrange
        var entity = CategoryGenerator.Generate();
        var request = new CreateCategoryRequest(entity.Name, entity.Description);

        _mockContext.Setup(c => c.Categories.Add(
            It.Is<Category>(e => e.Name == request.Name && e.Description == request.Description)));

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);

        _mockValidator.Verify(
            v => v.ValidateAndThrow(It.IsAny<CreateCategoryRequest>()),
            Times.Once);
        _mockContext.Verify(c => c.Categories.Add(
            It.IsAny<Category>()),
            Times.Once);
        _mockContext.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

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

        _mockValidator.Verify(
            v => v.ValidateAndThrow(It.IsAny<UpdateCategoryRequest>()),
            Times.Once);
        _mockContext.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
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

        _mockContext.Verify(
            c => c.Categories.FindAsync(It.IsAny<int>()),
            Times.Once);
        _mockContext.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedCategory_WhenRequestIsValid()
    {
        // Arrange
        var categoryToUpdate = CategoryGenerator.Generate();
        var request = _fixture.Create<UpdateCategoryRequest>();

        _mockContext.Setup(c => c.Categories.FindAsync(request.Id))
            .ReturnsAsync(categoryToUpdate);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        Assert.Equal(request.Name, response.Name);
        Assert.Equal(request.Description, response.Description);

        _mockContext.Verify(
            c => c.Categories.FindAsync(It.IsAny<int>()),
            Times.Once);
        _mockContext.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

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

        _mockContext.Verify(
            c => c.Categories.FindAsync(It.IsAny<int>()),
            Times.Never);
        _mockContext.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
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

        _mockContext.Verify(
            c => c.Categories.FindAsync(It.IsAny<int>()),
            Times.Once);
        _mockContext.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
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
        _mockContext.Verify(
            c => c.Categories.FindAsync(It.IsAny<int>()),
            Times.Once);
        _mockContext.Verify(
            c => c.Categories.Remove(It.IsAny<Category>()),
            Times.Once);
        _mockContext.Verify(
            c => c.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private void SetupCategories(IEnumerable<Category> categories)
    {
        var categoriesList = categories.ToList();
        for (int i = 0; i < categoriesList.Count; i++)
        {
            categoriesList[i].Id = i + 1;
        }

        var mockDbSet = categoriesList.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(c => c.Categories)
            .Returns(mockDbSet.Object);
    }
}
