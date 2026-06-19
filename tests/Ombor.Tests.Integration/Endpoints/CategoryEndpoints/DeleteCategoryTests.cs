using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.CategoryEndpoints;

public class DeleteCategoryTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : CategoryTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenCategoryExists()
    {
        // Arrange
        var categoryToDelete = _builder.CategoryBuilder
            .WithName("Category To Delete")
            .Build();
        var categoryId = await CreateCategoryAsync(categoryToDelete);
        var url = GetUrl(categoryId);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Category.ValidateDeleteAsync(categoryId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Category>(NonExistentEntityId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnConflictAndNotCascade_WhenCategoryHasProducts()
    {
        // Arrange
        var categoryId = await CreateCategoryAsync();
        var productId = await CreateProductAsync(categoryId);
        var url = GetUrl(categoryId);

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(url, HttpStatusCode.Conflict);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, response.Status);
        Assert.Contains("1", response.Detail);

        // The category and its product must both survive — never a silent cascade delete.
        Assert.True(await _context.Categories.AnyAsync(c => c.Id == categoryId));
        Assert.True(await _context.Products.AnyAsync(p => p.Id == productId));
    }

    private async Task<int> CreateProductAsync(int categoryId)
    {
        var product = new Product
        {
            Name = "Referencing Product",
            SKU = $"SKU {Guid.NewGuid()}",
            Measurement = UnitOfMeasurement.Unit,
            CategoryId = categoryId,
            Category = null!,
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }
}
