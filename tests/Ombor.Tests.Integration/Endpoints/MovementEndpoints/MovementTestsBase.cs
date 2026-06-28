using System.Net;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Contracts.Responses.Transfer;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.MovementEndpoints;

public abstract class MovementTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Warehouse;
    protected override string GetUrl(int id) => $"{Routes.Warehouse}/{id}";

    protected async Task<int> CreateWarehouseAsync()
    {
        var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent" };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return warehouse.Id;
    }

    protected async Task<int> CreateProductAsync()
    {
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            SalePrice = 100m,
            SupplyPrice = 50m,
            RetailPrice = 90m,
            LowStockThreshold = 10,
            Measurement = UnitOfMeasurement.Unit,
            Type = ProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }

    protected async Task<int> CreatePartnerAsync()
    {
        var partner = new Partner { Name = $"Partner {Guid.NewGuid():N}", Type = PartnerType.Both };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    protected async Task<int> CreateWalletAsync()
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = 1_000_000m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return wallet.Id;
    }

    protected Task AddOpeningStockAsync(int warehouseId, int productId, int quantity, decimal unitCost) =>
        _client.PostAsync<WarehouseDto>(
            $"{Routes.Warehouse}/{warehouseId}/opening-stock",
            new { warehouseId, items = new[] { new { productId, quantity, unitCost } } },
            HttpStatusCode.OK);

    protected async Task<int> PostTransactionAsync(CreateTransactionRequest request)
    {
        var form = request.ToMultipartFormData();
        var transaction = await _client.PostAsync<TransactionDto>(Routes.Transaction, form, HttpStatusCode.Created);

        return transaction.Id;
    }

    protected Task AddDecreaseAdjustmentAsync(int warehouseId, int productId, int quantity, string reason = "Damage") =>
        _client.PostAsync<StockAdjustmentDto>(
            Routes.StockAdjustment,
            new { warehouseId, productId, direction = "Decrease", quantity, reason, note = (string?)null },
            HttpStatusCode.Created);

    protected Task AddTransferAsync(int fromWarehouseId, int toWarehouseId, int productId, int quantity) =>
        _client.PostAsync<TransferDto>(
            Routes.Transfer,
            new { fromWarehouseId, toWarehouseId, note = (string?)null, lines = new[] { new { productId, quantity } } },
            HttpStatusCode.Created);
}
