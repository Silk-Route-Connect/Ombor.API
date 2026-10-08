using System.Net;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Contracts.Responses.Payment;
using Ombor.Contracts.Responses.Product;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Contracts.Responses.Transfer;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.MovementEndpoints;

/// <summary>
/// Every row that points at a document serves that document's number (not only its id, which differs) and, for
/// stock movements, the routable document id — so a row can show «№N» and open its source.
/// </summary>
public sealed class DocumentNumberServingTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : MovementTestsBase(factory, output)
{
    [Fact]
    public async Task ProductTransactions_ServeTheTransactionNumber()
    {
        var (product, warehouse, partner, _) = await ArrangeAsync();
        var sale = await PostSaleAsync(partner, product, warehouse, paid: 0m);

        var rows = await _client.GetAsync<ProductTransactionDto[]>($"{Routes.Product}/{product}/transactions");

        var row = Assert.Single(rows, r => r.Id == sale.Id);
        Assert.NotNull(sale.Number);
        Assert.Equal(sale.Number, row.TransactionNumber);
    }

    [Fact]
    public async Task Outstanding_ServesTheTransactionNumber()
    {
        var (product, warehouse, partner, _) = await ArrangeAsync();
        var sale = await PostSaleAsync(partner, product, warehouse, paid: 0m);

        var rows = await _client.GetAsync<OutstandingTransactionDto[]>($"payments/outstanding?partnerId={partner}");

        Assert.Equal(sale.Number, Assert.Single(rows, r => r.Id == sale.Id).Number);
    }

    [Fact]
    public async Task PaymentAllocation_ServesTheSettledTransactionNumber()
    {
        var (product, warehouse, partner, wallet) = await ArrangeAsync();
        var sale = await PostSaleAsync(partner, product, warehouse, paid: 0m);
        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partner, null, wallet,
            Amount: 400m, Description: null, Period: null,
            Settlements: [new SettlementInput(sale.Id, 400m)]);

        var payment = await _client.PostAsync<PaymentRecordDto>("payments", request.ToMultipartFormData());

        var allocation = Assert.Single(payment.Allocations);
        Assert.Equal(sale.Id, allocation.TransactionId);
        Assert.Equal(sale.Number, allocation.TransactionNumber);
    }

    [Fact]
    public async Task DashboardRecent_ServesNumberAndPartner()
    {
        var (product, warehouse, partner, _) = await ArrangeAsync();
        var sale = await PostSaleAsync(partner, product, warehouse, paid: 0m);

        var dashboard = await _client.GetAsync<DashboardDto>(Routes.Dashboard);

        var row = Assert.Single(dashboard.RecentTransactions, r => r.Id == sale.Id);
        Assert.Equal(sale.Number, row.TransactionNumber);
        Assert.Equal(partner, row.PartnerId);
    }

    [Fact]
    public async Task Movements_ServeTheRoutableSourceDocument()
    {
        // Arrange — one event of every source kind on the same product.
        var (product, source, partner, _) = await ArrangeAsync();
        var destination = await CreateWarehouseAsync();
        var sale = await PostSaleAsync(partner, product, source, paid: 0m);
        var adjustment = await _client.PostAsync<StockAdjustmentDto>(
            Routes.StockAdjustment,
            new { warehouseId = source, productId = product, direction = "Decrease", quantity = 2, reason = "Damage", note = (string?)null });
        var transfer = await _client.PostAsync<TransferDto>(
            Routes.Transfer,
            new { fromWarehouseId = source, toWarehouseId = destination, note = (string?)null, lines = new[] { new { productId = product, quantity = 5 } } });
        var openingId = await _context.OpeningStocks.AsNoTracking()
            .Where(o => o.WarehouseId == source && o.ProductId == product)
            .Select(o => o.Id)
            .SingleAsync();

        // Act
        var warehouseRows = await _client.GetAsync<WarehouseMovementDto[]>($"{Routes.Warehouse}/{source}/movements");
        var productRows = await _client.GetAsync<ProductMovementDto[]>($"{Routes.Product}/{product}/movements");

        // Assert — the warehouse ledger: the document id (not the line id), and a number only for the sale.
        var saleRow = warehouseRows.Single(m => m.Kind == MovementKind.Sale);
        Assert.Equal((MovementSource.Transaction, sale.Id, sale.Number), (saleRow.SourceType, saleRow.SourceId, saleRow.SourceNumber));
        var adjustmentRow = warehouseRows.Single(m => m.Kind == MovementKind.Adjustment);
        Assert.Equal((MovementSource.StockAdjustment, adjustment.Id, (string?)null), (adjustmentRow.SourceType, adjustmentRow.SourceId, adjustmentRow.SourceNumber));
        var transferRow = warehouseRows.Single(m => m.Kind == MovementKind.Transfer);
        Assert.Equal((MovementSource.Transfer, transfer.Id, (string?)null), (transferRow.SourceType, transferRow.SourceId, transferRow.SourceNumber));
        var openingRow = warehouseRows.Single(m => m.Kind == MovementKind.Opening);
        Assert.Equal((MovementSource.OpeningStock, openingId), (openingRow.SourceType, openingRow.SourceId));

        // Assert — the product ledger agrees; both transfer legs open the same transfer.
        Assert.Equal(sale.Number, productRows.Single(m => m.Kind == MovementKind.Sale).SourceNumber);
        Assert.All(productRows.Where(m => m.Kind == MovementKind.Transfer), m => Assert.Equal(transfer.Id, m.SourceId));
        Assert.Equal(2, productRows.Count(m => m.Kind == MovementKind.Transfer));
    }

    private async Task<(int Product, int Warehouse, int Partner, int Wallet)> ArrangeAsync()
    {
        var warehouse = await CreateWarehouseAsync();
        var product = await CreateProductAsync();
        var partner = await CreatePartnerAsync();
        var wallet = await CreateWalletAsync();
        await AddOpeningStockAsync(warehouse, product, quantity: 100, unitCost: 10m);

        return (product, warehouse, partner, wallet);
    }

    private Task<TransactionDto> PostSaleAsync(int partner, int product, int warehouse, decimal paid) =>
        _client.PostAsync<TransactionDto>(
            Routes.Transaction,
            TransactionRequestFactory.Sale(partner, product, warehouse, due: 1_000m, walletId: null, paidAmount: paid).ToMultipartFormData(),
            HttpStatusCode.Created);
}
