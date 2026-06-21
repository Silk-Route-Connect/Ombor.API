using System.Net;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.Product;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.MovementEndpoints;

public sealed class MovementLedgerTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : MovementTestsBase(factory, output)
{
    [Fact]
    public async Task WarehouseMovements_CoverEveryKind_AndReconcileToLiveStock()
    {
        // Arrange — drive every stock event through its real flow.
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();
        var product = await CreateProductAsync();
        var partner = await CreatePartnerAsync();
        var wallet = await CreateWalletAsync();

        await AddOpeningStockAsync(source, product, quantity: 100, unitCost: 10m);                                          // +100 -> 100
        await PostTransactionAsync(TransactionRequestFactory.Supply(partner, product, source, due: 1_000m, wallet, 1_000m)); //   +1 -> 101
        await PostTransactionAsync(TransactionRequestFactory.Sale(partner, product, source, due: 1_000m, wallet, 1_000m));   //   -1 -> 100
        await AddDecreaseAdjustmentAsync(source, product, quantity: 5);                                                      //   -5 ->  95
        await AddTransferAsync(source, destination, product, quantity: 20);                                                  //  -20 ->  75

        // Act
        var movements = await _client.GetAsync<WarehouseMovementDto[]>($"{Routes.Warehouse}/{source}/movements");

        // Assert — every kind is present with the correct signed delta.
        Assert.Equal(100, movements.Single(m => m.Kind == "Opening").Quantity);
        Assert.Equal(1, movements.Single(m => m.Kind == "Supply").Quantity);
        Assert.Equal(-1, movements.Single(m => m.Kind == "Sale").Quantity);
        Assert.Equal(-5, movements.Single(m => m.Kind == "Adjustment").Quantity);

        var transfer = movements.Single(m => m.Kind == "Transfer");
        Assert.Equal(-20, transfer.Quantity);
        Assert.False(string.IsNullOrEmpty(transfer.Counterparty)); // the destination warehouse name
        Assert.False(string.IsNullOrEmpty(movements.Single(m => m.Kind == "Supply").Counterparty)); // the partner name

        // Newest-first: the transfer was the last event.
        Assert.Equal("Transfer", movements[0].Kind);

        // The ledger reconciles to live stock: the last event's running balance == WarehouseItem.Quantity.
        var item = await _context.WarehouseItems.AsNoTracking()
            .FirstAsync(i => i.WarehouseId == source && i.ProductId == product);
        Assert.Equal(75, item.Quantity);
        Assert.Equal(item.Quantity, transfer.BalanceAfter);
    }

    [Fact]
    public async Task ProductMovements_SpanWarehouses_AndShowTransferOnBothSides()
    {
        // Arrange
        var source = await CreateWarehouseAsync();
        var destination = await CreateWarehouseAsync();
        var product = await CreateProductAsync();

        await AddOpeningStockAsync(source, product, quantity: 100, unitCost: 10m); // source 100
        await AddTransferAsync(source, destination, product, quantity: 30);        // source 70, destination 30

        // Act
        var movements = await _client.GetAsync<ProductMovementDto[]>($"{Routes.Product}/{product}/movements");

        // Assert — the transfer is two movements: send at the source, receive at the destination.
        Assert.Equal(2, movements.Count(m => m.Kind == "Transfer"));
        var send = movements.Single(m => m.Kind == "Transfer" && m.WarehouseId == source);
        var receive = movements.Single(m => m.Kind == "Transfer" && m.WarehouseId == destination);
        Assert.Equal(-30, send.Quantity);
        Assert.Equal(30, receive.Quantity);
        Assert.False(string.IsNullOrEmpty(receive.WarehouseName));

        // The running total reconciles to the product's total stock across warehouses.
        var totalStock = await _context.WarehouseItems.AsNoTracking()
            .Where(i => i.ProductId == product)
            .SumAsync(i => i.Quantity);
        Assert.Equal(100, totalStock); // 70 + 30 — a transfer doesn't change total stock
        Assert.Equal(totalStock, movements[0].BalanceAfter); // newest-first: the latest running total
    }

    [Fact]
    public async Task Movements_Return404_ForUnknownWarehouseOrProduct()
    {
        await _client.GetAsync($"{Routes.Warehouse}/999999/movements", HttpStatusCode.NotFound);
        await _client.GetAsync($"{Routes.Product}/999999/movements", HttpStatusCode.NotFound);
    }
}
