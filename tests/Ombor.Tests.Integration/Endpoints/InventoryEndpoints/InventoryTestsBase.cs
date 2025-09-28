using System.Net;
using Microsoft.EntityFrameworkCore.Metadata;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.InventoryEndpoints;

public abstract class InventoryTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : EndpointTestsBase(factory, outputHelper)
{
    protected readonly string _searchTerm = "Inventory 123";

    protected override string GetUrl()
        => Routes.Inventory;

    protected override string GetUrl(int id)
        => $"{Routes.Inventory}/{id}";

    protected async Task<Inventory> CreateInventoryAsync()
    {
        var inventory = new Inventory
        {
            Name = "Inventory 123",
            Location = "Tashkent",
            IsActive = true
        };

        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();

        return inventory;
    }

    protected async Task<int> CreateInventoryAsync(Inventory inventory)
    {
        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();

        return inventory.Id;
    }
}
