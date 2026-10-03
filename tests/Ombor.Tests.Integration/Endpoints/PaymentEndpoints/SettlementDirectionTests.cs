using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainEnums = Ombor.Domain.Enums;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

/// <summary>
/// Owner decision 2026-10-04 (narrows DR-05 to the obviously-wrong case): an Income payment settles only receivables
/// (Sale, SupplyRefund) and an Expense payment only payables (Supply, SaleRefund). Before this, money coming in could
/// "pay off" a supplier debt, counting the cash twice in our favour; the ledger then no longer explained the wallet.
/// </summary>
public sealed class SettlementDirectionTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task IncomePayment_ShouldNotSettleASupply_AndMoveNothing()
    {
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync(0m);
        var supplyId = await CreateOpenTransactionAsync(partnerId, DomainEnums.TransactionType.Supply, 5_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, partnerId, null, walletId,
            Amount: 5_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(supplyId, 5_000m)]);

        var problem = await _client.PostAsync<JObject>(GetUrl(), request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.Equal("payment.direction_mismatch", (string?)problem["code"]);
        Assert.NotNull(problem["errors"]?["Settlements[0].TransactionId"]);
        var supply = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == supplyId);
        Assert.Equal(0m, supply.TotalPaid);
        Assert.False(await _context.Payments.AnyAsync(p => p.WalletId == walletId));
    }

    [Fact]
    public async Task ExpensePayment_ShouldNotSettleASale()
    {
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync(10_000m);
        var saleId = await CreateOpenSaleAsync(partnerId, 5_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Expense, partnerId, null, walletId,
            Amount: 5_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(saleId, 5_000m)]);

        var problem = await _client.PostAsync<JObject>(GetUrl(), request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.Equal("payment.direction_mismatch", (string?)problem["code"]);
    }

    [Theory]
    [InlineData(PaymentDirection.Income, DomainEnums.TransactionType.SupplyRefund)]
    [InlineData(PaymentDirection.Expense, DomainEnums.TransactionType.Supply)]
    [InlineData(PaymentDirection.Expense, DomainEnums.TransactionType.SaleRefund)]
    public async Task Payment_ShouldSettle_WhenDirectionMatches(PaymentDirection direction, DomainEnums.TransactionType type)
    {
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync(10_000m);
        var transactionId = await CreateOpenTransactionAsync(partnerId, type, 2_000m);

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, direction, partnerId, null, walletId,
            Amount: 2_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(transactionId, 2_000m)]);

        var payment = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request.ToMultipartFormData());

        Assert.Equal(2_000m, Assert.Single(payment.Allocations).Amount);
        var settled = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == transactionId);
        Assert.Equal(2_000m, settled.TotalPaid);
    }

    [Fact]
    public async Task SaleCreate_ShouldNotSettleTheSameSuppliersPayable()
    {
        // A partner who is both customer and supplier: the cash paid for this sale must not "pay" our supply debt.
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync(0m);
        var supplyId = await CreateOpenTransactionAsync(partnerId, DomainEnums.TransactionType.Supply, 1_000m);
        var warehouseId = await EnsureWarehouseAsync();
        var productId = await CreateStockedProductAsync(warehouseId);

        var request = new CreateTransactionRequest(
            PartnerId: partnerId,
            Type: TransactionType.Sale,
            Notes: null,
            Lines: [new CreateTransactionLine(productId, UnitPrice: 1_000m, Discount: 0m, DiscountType.Fixed, Quantity: 1m)],
            WalletId: walletId,
            PaidAmount: 2_000m,
            Settlements: [new SettlementInput(supplyId, 1_000m)],
            Overpayment: OverpaymentHandling.Change,
            Attachments: [],
            WarehouseId: warehouseId);

        var problem = await _client.PostAsync<JObject>("transactions", request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.Equal("payment.direction_mismatch", (string?)problem["code"]);
        var supply = await _context.Transactions.AsNoTracking().FirstAsync(t => t.Id == supplyId);
        Assert.Equal(0m, supply.TotalPaid);
    }

    [Fact]
    public async Task StandalonePayroll_ShouldBeAnExpense()
    {
        var walletId = await CreateWalletAsync(10_000m);
        var employee = new Employee
        {
            FullName = $"Employee {Guid.NewGuid():N}",
            Position = "Cashier",
            Salary = 1_000m,
            Status = DomainEnums.EmployeeStatus.Active,
            DateOfEmployment = new DateOnly(2026, 1, 1),
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        var request = new CreatePaymentRecordRequest(
            PaymentType.Payroll, PaymentDirection.Income, null, employee.Id, walletId,
            Amount: 1_000m, Description: null, Period: "2026-10", Settlements: []);

        var problem = await _client.PostAsync<JObject>(GetUrl(), request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.NotNull(problem["errors"]?[nameof(CreatePaymentRecordRequest.Direction)]);
    }

    private async Task<int> CreateOpenTransactionAsync(int partnerId, DomainEnums.TransactionType type, decimal total)
    {
        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = type,
            WarehouseId = await EnsureWarehouseAsync(),
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = total,
            TotalPaid = 0m,
            Status = DomainEnums.TransactionStatus.Open,
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction.Id;
    }

    private async Task<int> CreateStockedProductAsync(int warehouseId)
    {
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            SalePrice = 1_000m,
            SupplyPrice = 500m,
            Measurement = DomainEnums.UnitOfMeasurement.Piece,
            Type = DomainEnums.ProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _context.WarehouseItems.Add(new WarehouseItem
        {
            WarehouseId = warehouseId,
            ProductId = product.Id,
            Quantity = 10m,
            AverageCost = 500m,
            Warehouse = null!,
            Product = null!,
        });
        await _context.SaveChangesAsync();

        return product.Id;
    }
}
