using Ombor.Application.Mappings;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Mappings;

public class CategoryMappingsTests
{
    [Fact]
    public void ToEntity_ShouldMapNameAndDescription()
    {
        // Arrange
        var request = new CreateCategoryRequest("Test Category", "Test Description");

        // Act
        var entity = request.ToEntity();

        // Assert
        Assert.Equal(request.Name, entity.Name);
        Assert.Equal(request.Description, entity.Description);
    }

    [Fact]
    public void ToCreateResponse_ShouldMapAllFields()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Test Category",
            Description = "Test Description"
        };

        // Act
        var response = category.ToCreateResponse();

        // Assert
        Assert.Equal(category.Id, response.Id);
        Assert.Equal(category.Name, response.Name);
        Assert.Equal(category.Description, response.Description);
    }

    [Fact]
    public void ToUpdateResponse_ShouldMapAllFields()
    {
        // Arrange
        var category = new Category
        {
            Id = 2,
            Name = "Test Category",
            Description = "Test Description"
        };

        // Act
        var response = category.ToUpdateResponse();

        // Assert
        Assert.Equal(category.Id, response.Id);
        Assert.Equal(category.Name, response.Name);
        Assert.Equal(category.Description, response.Description);
    }

    [Fact]
    public void ToDto_ShouldMapAllFields()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Test Category",
            Description = "Test Description"
        };

        // Act
        var dto = category.ToDto();

        // Assert
        Assert.Equal(category.Id, dto.Id);
        Assert.Equal(category.Name, dto.Name);
        Assert.Equal(category.Description, dto.Description);
    }

    [Fact]
    public void ApplyUpdate_ShouldOverwriteNameAndDescription()
    {
        // Arrange
        var category = new Category
        {
            Id = 1,
            Name = "Test Category",
            Description = "Test Description"
        };

        var request = new UpdateCategoryRequest(1, "Updated Name", "Upated Description");

        // Act
        category.ApplyUpdate(request);

        // Assert
        Assert.Equal(request.Name, category.Name);
        Assert.Equal(request.Description, category.Description);
    }
}
