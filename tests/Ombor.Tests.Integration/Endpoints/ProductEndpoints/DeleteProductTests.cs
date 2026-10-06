using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Template;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class DeleteProductTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenProductExists()
    {
        // Arrange
        var images = new string[] { "product-3.jpg", "product-4.jpg" };
        var productId = await CreateProductAsync(DefaultCategoryId, images);
        var url = GetUrl(productId);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Product.ValidateDeleteAsync(productId, images);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnConflict_WhenOnlyOpeningStockReferencesTheProduct()
    {
        // Arrange — no sale or order line: the opening stock event alone references the product (Restrict FK),
        // which the narrower predicate missed, so the delete surfaced the database error as a 500.
        var productId = await CreateProductAsync(DefaultCategoryId);
        var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent" };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();
        _context.OpeningStocks.Add(new OpeningStock
        {
            DateUtc = DateTimeOffset.UtcNow,
            WarehouseId = warehouse.Id,
            Warehouse = null!,
            ProductId = productId,
            Product = null!,
            Quantity = 5m,
            UnitCost = 10m,
        });
        await _context.SaveChangesAsync();

        // Act
        var problem = await _client.DeleteAsync<JObject>(GetUrl(productId), HttpStatusCode.Conflict);
        var product = await _client.GetAsync<JObject>(GetUrl(productId));

        // Assert — 409 entity.referenced, and the served flag agrees with the guard.
        Assert.Equal("entity.referenced", (string?)problem["code"]);
        Assert.False((bool?)product["isDeletable"]);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnConflict_WhenOnlyATemplateItemReferencesTheProduct()
    {
        // Arrange
        var productId = await CreateProductAsync(DefaultCategoryId);
        var partner = new Partner { Name = $"Partner {Guid.NewGuid():N}", Type = Ombor.Domain.Enums.PartnerType.Both };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();
        await _client.PostAsync<JObject>(
            "templates",
            new CreateTemplateRequest(partner.Id, $"Template {Guid.NewGuid():N}", TemplateType.Sale, [new CreateTemplateItem(productId, 1m, 100m, 0m)]));

        // Act & Assert
        var problem = await _client.DeleteAsync<JObject>(GetUrl(productId), HttpStatusCode.Conflict);
        Assert.Equal("entity.referenced", (string?)problem["code"]);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Product>(NonExistentEntityId);
    }
}
