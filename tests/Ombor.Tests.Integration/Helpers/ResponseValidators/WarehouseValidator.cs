using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Integration.Helpers.ResponseValidators;

public sealed class WarehouseValidator(IApplicationDbContext context)
{
    public async Task ValidateGetAsync(GetWarehousesRequest request, WarehouseDto[] response)
    {
        var expectedWarehouses = await GetWarehousesAsync(request);

        Assert.Equal(expectedWarehouses.Length, response.Length);
        Assert.All(expectedWarehouses, expected =>
        {
            var actual = response.FirstOrDefault(x => x.Id == expected.Id);

            WarehouseAssertionHelper.AssertEquivalent(expected, actual);
        });
    }

    public async Task ValidateGetByIdAsync(int warehouseId, WarehouseDto response)
    {
        var expected = await GetWithItemsAsync(warehouseId);

        WarehouseAssertionHelper.AssertEquivalent(expected, response);
    }

    public async Task ValidatePostAsync(CreateWarehouseRequest request, WarehouseDto response)
    {
        var warehouse = await GetWithItemsAsync(response.Id);

        WarehouseAssertionHelper.AssertEquivalent(request, warehouse);
        WarehouseAssertionHelper.AssertEquivalent(request, response);
        WarehouseAssertionHelper.AssertEquivalent(warehouse, response);
    }

    public async Task ValidatePutAsync(UpdateWarehouseRequest request, WarehouseDto response)
    {
        var warehouse = await GetWithItemsAsync(response.Id);

        WarehouseAssertionHelper.AssertEquivalent(request, warehouse);
        WarehouseAssertionHelper.AssertEquivalent(request, response);
        WarehouseAssertionHelper.AssertEquivalent(warehouse, response);
    }

    public async Task ValidateDeleteAsync(int warehouseId)
    {
        var warehouse = await context.Warehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == warehouseId);

        Assert.Null(warehouse);
    }

    private async Task<Warehouse?> GetWithItemsAsync(int warehouseId) =>
        await context.Warehouses
            .AsNoTracking()
            .Include(x => x.WarehouseItems)
            .FirstOrDefaultAsync(x => x.Id == warehouseId);

    private async Task<Warehouse[]> GetWarehousesAsync(GetWarehousesRequest request)
    {
        var query = context.Warehouses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.Name.Contains(request.SearchTerm) ||
            (x.Location != null && x.Location.Contains(request.SearchTerm)));
        }

        return await query
            .Include(x => x.WarehouseItems)
            .OrderBy(x => x.Name)
            .ToArrayAsync();
    }
}
