using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

public class DeleteWarehouseTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : WarehouseTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenWarehouseUnreferenced()
    {
        // Arrange — a brand-new warehouse with no stock, movements, transfers, transactions, or orders.
        var warehouse = await CreateWarehouseAsync();

        // It is reported deletable before deletion.
        var fetched = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id));
        Assert.True(fetched.IsDeletable);

        // Act
        await _client.DeleteAsync(GetUrl(warehouse.Id));

        // Assert
        await _responseValidator.Warehouse.ValidateDeleteAsync(warehouse.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenWarehouseDoesNotExist()
    {
        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Warehouse>(NonExistentEntityId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnConflictAndNotCascade_WhenWarehouseHasStock()
    {
        // Arrange — a warehouse holding stock is referenced (rule 32).
        var warehouse = await CreateWarehouseAsync();
        var product = await CreateProductAsync();
        await AddOpeningStockAsync(warehouse.Id, product.Id);

        // It is reported non-deletable.
        var fetched = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id));
        Assert.False(fetched.IsDeletable);

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(GetUrl(warehouse.Id), HttpStatusCode.Conflict);

        // Assert
        Assert.Equal((int)HttpStatusCode.Conflict, response.Status);

        // The warehouse and its stock must both survive — never a silent cascade delete.
        Assert.True(await _context.Warehouses.AnyAsync(w => w.Id == warehouse.Id));
        Assert.True(await _context.WarehouseItems.AnyAsync(i => i.WarehouseId == warehouse.Id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnConflict_WhenArchivedWarehouseStillHoldsStock()
    {
        // Arrange — archiving a stock-holding warehouse does not make it deletable (rules 31 & 32).
        var warehouse = await CreateWarehouseAsync();
        var product = await CreateProductAsync();
        await AddOpeningStockAsync(warehouse.Id, product.Id);
        await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouse.Id)}/archive", content: new { }, HttpStatusCode.OK);

        // Even archived, it is reported non-deletable.
        var fetched = await _client.GetAsync<WarehouseDto>(GetUrl(warehouse.Id));
        Assert.True(fetched.IsArchived);
        Assert.False(fetched.IsDeletable);

        // Act
        await _client.DeleteAsync<ProblemDetails>(GetUrl(warehouse.Id), HttpStatusCode.Conflict);

        // Assert — it survives.
        Assert.True(await _context.Warehouses.AnyAsync(w => w.Id == warehouse.Id));
    }

    private async Task AddOpeningStockAsync(int warehouseId, int productId)
    {
        var request = new AddOpeningStockRequest(
            warehouseId,
            [new OpeningStockLine(productId, Quantity: 10, UnitCost: 100m)]);

        await _client.PostAsync<WarehouseDto>($"{GetUrl(warehouseId)}/opening-stock", request, HttpStatusCode.OK);
    }
}
