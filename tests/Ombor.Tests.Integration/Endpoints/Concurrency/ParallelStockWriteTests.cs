using System.Net;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.Order;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using TransactionType = Ombor.Contracts.Enums.TransactionType;

namespace Ombor.Tests.Integration.Endpoints.Concurrency;

/// <summary>
/// Rule 20 under parallel requests: however many writes race for the same units, each unit leaves the warehouse
/// once and the stock never goes below zero (backend-7).
/// </summary>
public sealed class ParallelStockWriteTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ConcurrencyTestsBase(factory, output)
{
    [Fact]
    public async Task ParallelSales_OfTheLastUnits_SellEachUnitOnce()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 5);

        var outcomes = await FireAsync(_ => Form(
            Routes.Transaction,
            TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 100m, walletId: null, paidAmount: 0m)
                .ToMultipartFormData()));

        AssertOutcomes(outcomes, succeeded: 5, HttpStatusCode.Created, HttpStatusCode.BadRequest, "stock.insufficient");
        Assert.Equal(0m, await StockOfAsync(warehouseId, productId));
        Assert.Equal(5m, await _context.TransactionLines
            .Where(l => l.ProductId == productId && l.Transaction.WarehouseId == warehouseId)
            .SumAsync(l => l.Quantity));
    }

    [Fact]
    public async Task ParallelRefunds_OfOneSale_NeverRefundMoreThanWasSold()
    {
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10);

        var saleRequest = TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 100m, walletId: null, paidAmount: 0m);
        saleRequest = saleRequest with { Lines = [saleRequest.Lines[0] with { Quantity = 3 }] };
        var sale = await _client.PostAsync<TransactionDto>(Routes.Transaction, saleRequest.ToMultipartFormData(), HttpStatusCode.Created);

        var outcomes = await FireAsync(_ => Form(
            Routes.Transaction,
            TransactionRequestFactory.Refund(TransactionType.SaleRefund, partnerId, productId, warehouseId, sale.Id, due: 100m)
                .ToMultipartFormData()));

        // The refund cap answers with a plain field error (validation.failed), not a domain code.
        AssertOutcomes(outcomes, succeeded: 3, HttpStatusCode.Created, HttpStatusCode.BadRequest, "validation.failed");
        Assert.Equal(3m, await _context.Transactions
            .Where(t => t.OriginalTransactionId == sale.Id)
            .SelectMany(t => t.Lines)
            .SumAsync(l => l.Quantity));
        Assert.Equal(10m, await StockOfAsync(warehouseId, productId));
    }

    [Fact]
    public async Task ParallelStockDecreases_NeverDriveStockNegative()
    {
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 4);

        var outcomes = await FireAsync(_ => Json(Routes.StockAdjustment, new
        {
            warehouseId,
            productId,
            direction = "Decrease",
            quantity = 1m,
            reason = "Damage",
            note = (string?)null,
        }));

        AssertOutcomes(outcomes, succeeded: 4, HttpStatusCode.Created, HttpStatusCode.BadRequest, "stock.insufficient");
        Assert.Equal(0m, await StockOfAsync(warehouseId, productId));
        Assert.Equal(4, await _context.StockAdjustments.CountAsync(a => a.WarehouseId == warehouseId && a.ProductId == productId));
    }

    [Fact]
    public async Task ParallelWarehouseTransfers_NeverSendMoreThanTheSourceHolds()
    {
        var sourceId = await CreateWarehouseAsync();
        var destinationId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(sourceId, productId, quantity: 3);

        var outcomes = await FireAsync(_ => Json(Routes.Transfer, new
        {
            fromWarehouseId = sourceId,
            toWarehouseId = destinationId,
            note = (string?)null,
            lines = new[] { new { productId, quantity = 1m } },
        }));

        AssertOutcomes(outcomes, succeeded: 3, HttpStatusCode.Created, HttpStatusCode.BadRequest, "stock.insufficient");
        Assert.Equal(0m, await StockOfAsync(sourceId, productId));
        Assert.Equal(3m, await StockOfAsync(destinationId, productId));
    }

    [Fact]
    public async Task ParallelDeliveries_OfOneOrder_PromoteItToOneSale()
    {
        var customerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10);

        var order = await _client.PostAsync<OrderDto>(Routes.Order, new
        {
            customerId,
            source = "OmborWeb",
            deliveryAddress = "Tashkent",
            notes = "Parallel delivery",
            lines = new[] { new { productId, quantity = 2, unitPrice = 100m, discountType = "Fixed" } },
        });
        await _client.PostAsync<OrderDto>($"{Routes.Order}/{order.Id}/process", new { }, HttpStatusCode.OK);
        await _client.PostAsync<OrderDto>($"{Routes.Order}/{order.Id}/ship", new { }, HttpStatusCode.OK);

        var outcomes = await FireAsync(_ => Json($"{Routes.Order}/{order.Id}/deliver", new { warehouseId }), count: 5);

        // The losers see the order already delivered: an illegal Delivered → Delivered transition (409, no code yet).
        AssertOutcomes(outcomes, succeeded: 1, HttpStatusCode.OK, HttpStatusCode.Conflict, refusedCode: null);
        Assert.Equal(8m, await StockOfAsync(warehouseId, productId));
        Assert.Equal(1, await _context.Transactions.CountAsync(t => t.PartnerId == customerId));
    }
}
