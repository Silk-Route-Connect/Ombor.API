using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.DebtEndpoints;

public sealed class DebtTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : DebtTestsBase(factory, output)
{
    [Fact]
    public async Task GetDebts_UnpaidSale_AppearsAsReceivableWithDerivedNumber()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10, unitCost: 50m);

        // Act
        var sale = await PostTransactionAsync(
            TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId: null, paidAmount: 0m));
        var debt = Assert.Single(await GetDebtsAsync(), d => d.TransactionId == sale.Id);

        // Assert
        Assert.Equal("Receivable", debt.Direction);
        Assert.Equal("Sale", debt.TransactionType);
        Assert.Equal($"S-{sale.Id}", debt.Number);
        Assert.Equal(partnerId, debt.PartnerId);
        Assert.Equal("Acme LLC", debt.PartnerCompany);
        Assert.Equal(1_000m, debt.Total);
        Assert.Equal(0m, debt.Paid);
        Assert.Equal(1_000m, debt.Remaining);
        Assert.Null(debt.DueDate);
        Assert.Equal(0, debt.OverdueDays);
        Assert.Equal(0, debt.AgeDays);
    }

    [Fact]
    public async Task GetDebts_UnpaidSupply_AppearsAsPayable()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();

        // Act — a Supply adds stock, so it needs none beforehand.
        var supply = await PostTransactionAsync(
            TransactionRequestFactory.Supply(partnerId, productId, warehouseId, due: 600m, walletId: null, paidAmount: 0m));
        var debt = Assert.Single(await GetDebtsAsync(), d => d.TransactionId == supply.Id);

        // Assert
        Assert.Equal("Payable", debt.Direction);
        Assert.Equal("Supply", debt.TransactionType);
        Assert.Equal($"SP-{supply.Id}", debt.Number);
        Assert.Equal(600m, debt.Remaining);
    }

    [Fact]
    public async Task GetDebts_PartiallyPaidSale_ListsRemaining()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        var walletId = await CreateWalletAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10, unitCost: 50m);

        // Act
        var sale = await PostTransactionAsync(
            TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId, paidAmount: 400m));
        var debt = Assert.Single(await GetDebtsAsync(), d => d.TransactionId == sale.Id);

        // Assert
        Assert.Equal(1_000m, debt.Total);
        Assert.Equal(400m, debt.Paid);
        Assert.Equal(600m, debt.Remaining);
    }

    [Fact]
    public async Task GetDebts_FullyPaidSale_IsExcluded()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        var walletId = await CreateWalletAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10, unitCost: 50m);

        // Act
        var sale = await PostTransactionAsync(
            TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId, paidAmount: 1_000m));
        var debts = await GetDebtsAsync();

        // Assert — a closed (fully settled) transaction carries no outstanding debt.
        Assert.DoesNotContain(debts, d => d.TransactionId == sale.Id);
    }

    [Fact]
    public async Task GetDebts_DueDateFlowsThroughCreate_AndDrivesOverdueDays()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10, unitCost: 50m);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Act
        var overdue = await PostTransactionAsync(TransactionRequestFactory.Sale(
            partnerId, productId, warehouseId, due: 1_000m, walletId: null, paidAmount: 0m, dueDate: today.AddDays(-5)));
        var notYetDue = await PostTransactionAsync(TransactionRequestFactory.Sale(
            partnerId, productId, warehouseId, due: 1_000m, walletId: null, paidAmount: 0m, dueDate: today.AddDays(5)));
        var debts = await GetDebtsAsync();

        // Assert — the due date round-trips through create, and overdue counts only past it.
        var overdueDebt = Assert.Single(debts, d => d.TransactionId == overdue.Id);
        Assert.Equal(today.AddDays(-5), overdueDebt.DueDate);
        Assert.Equal(5, overdueDebt.OverdueDays);

        var notYetDueDebt = Assert.Single(debts, d => d.TransactionId == notYetDue.Id);
        Assert.Equal(today.AddDays(5), notYetDueDebt.DueDate);
        Assert.Equal(0, notYetDueDebt.OverdueDays);
    }

    [Fact]
    public async Task GetDebts_AgeAndOverdueDays_ComputeFromDates()
    {
        // Arrange — seed directly so the transaction can be back-dated.
        var partnerId = await CreatePartnerAsync();
        var now = DateTimeOffset.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var id = await AddOutstandingTransactionAsync(
            _context, partnerId, TransactionType.Sale,
            totalDue: 1_000m, totalPaid: 0m, dateUtc: now.AddDays(-40), dueDate: today.AddDays(-10));

        // Act
        var debt = Assert.Single(await GetDebtsAsync(), d => d.TransactionId == id);

        // Assert
        Assert.Equal(40, debt.AgeDays);
        Assert.Equal(10, debt.OverdueDays);
        Assert.Equal("Receivable", debt.Direction);
        Assert.Equal($"S-{id}", debt.Number);
    }

    [Fact]
    public async Task GetDebts_ReconcileWithPartnerBalanceView()
    {
        // Arrange — a receivable, a payable, and a partially-paid receivable for one partner.
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var warehouseId = await CreateWarehouseAsync();
        var walletId = await CreateWalletAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 100, unitCost: 50m);

        await PostTransactionAsync(TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId: null, paidAmount: 0m));
        await PostTransactionAsync(TransactionRequestFactory.Supply(partnerId, productId, warehouseId, due: 600m, walletId: null, paidAmount: 0m));
        await PostTransactionAsync(TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId, paidAmount: 400m));

        // Act
        var debts = (await GetDebtsAsync()).Where(d => d.PartnerId == partnerId).ToArray();
        var receivable = debts.Where(d => d.Direction == "Receivable").Sum(d => d.Remaining);
        var payable = debts.Where(d => d.Direction == "Payable").Sum(d => d.Remaining);
        var balance = await _context.PartnerBalances.FirstAsync(b => b.PartnerId == partnerId);

        // Assert — the debt list sums to the same figures the partner-balance view derives.
        Assert.Equal(balance.ReceivableDebt, receivable);
        Assert.Equal(balance.PayableDebt, payable);
        Assert.Equal(1_600m, receivable);
        Assert.Equal(600m, payable);
    }

    [Fact]
    public async Task GetDebts_AreScopedToOrganization()
    {
        // Arrange — an outstanding transaction in the caller's org (1) and one in org 2.
        var org1PartnerId = await CreatePartnerAsync();
        var org1Id = await AddOutstandingTransactionAsync(
            _context, org1PartnerId, TransactionType.Sale, 1_000m, 0m, DateTimeOffset.UtcNow, dueDate: null);

        await using var org2 = CreateContext(2);
        var org2PartnerId = await AddPartnerAsync(org2);
        var org2Id = await AddOutstandingTransactionAsync(
            org2, org2PartnerId, TransactionType.Sale, 5_000m, 0m, DateTimeOffset.UtcNow, dueDate: null);

        // Act
        var debts = await GetDebtsAsync();

        // Assert — the global query filter keeps org 2's debt out of org 1's list.
        Assert.Contains(debts, d => d.TransactionId == org1Id);
        Assert.DoesNotContain(debts, d => d.TransactionId == org2Id);
    }
}
