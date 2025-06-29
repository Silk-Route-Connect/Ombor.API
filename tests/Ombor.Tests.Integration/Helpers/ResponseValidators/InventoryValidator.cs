using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class InventoryValidator(IApplicationDbContext context)
{
    public async Task ValidateGetAsync(GetInventoriesRequest request, InventoryDto[] response)
    {
        var expectedInventories = await GetInventoriesAsync(request);

        Assert.Equal(expectedInventories.Length, response.Length);
        Assert.All(expectedInventories, expected =>
        {
            var actual = response.FirstOrDefault(x => x.Id == expected.Id);

            InventoryAssertionHelper.AssertEquivalent(expected, actual);
        });
    }

    public async Task ValidateGetByIdAsync(int inventoryId, InventoryDto response)
    {
        var expected = await context.Inventories
            .FirstOrDefaultAsync(x => x.Id == inventoryId);

        InventoryAssertionHelper.AssertEquivalent(expected, response);
    }

    public async Task ValidatePostAsync(CreateInventoryRequest request, CreateInventoryResponse response)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(x => x.Id == response.Id);

        InventoryAssertionHelper.AssertEquivalent(request, inventory);
        InventoryAssertionHelper.AssertEquivalent(request, response);
        InventoryAssertionHelper.AssertEquivalent(inventory, response);
    }

    public async Task ValidatePutAsync(UpdateInventoryRequest request, UpdateInventoryResponse response)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(x => x.Id == response.Id);

        InventoryAssertionHelper.AssertEquivalent(request, inventory);
        InventoryAssertionHelper.AssertEquivalent(request, response);
        InventoryAssertionHelper.AssertEquivalent(inventory, response);
    }

    public async Task ValidateDeleteAsync(int inventoryId)
    {
        var inventory = await context.Inventories
            .FirstOrDefaultAsync(x => x.Id == inventoryId);

        Assert.NotNull(inventory);
    }

    private async Task<Inventory[]> GetInventoriesAsync(GetInventoriesRequest request)
    {
        var query = context.Inventories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Name.Contains(request.SearchTerm) ||
            (x.Location != null && x.Location.Contains(request.SearchTerm)));
        }

        return await query
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToArrayAsync();
    }
}
