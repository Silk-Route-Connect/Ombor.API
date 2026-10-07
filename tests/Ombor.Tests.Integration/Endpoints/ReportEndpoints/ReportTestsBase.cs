using System.Globalization;
using System.Net;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.StockAdjustment;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainEmployeeStatus = Ombor.Domain.Enums.EmployeeStatus;
using DomainPartnerType = Ombor.Domain.Enums.PartnerType;
using DomainProductType = Ombor.Domain.Enums.ProductType;
using DomainUnit = Ombor.Domain.Enums.UnitOfMeasurement;
using DomainWalletType = Ombor.Domain.Enums.WalletType;

namespace Ombor.Tests.Integration.Endpoints.ReportEndpoints;

/// <summary>
/// Report tests share the collection database with every other test, so each one works on fresh products, partners,
/// warehouses and wallets and reads its own row — or compares a report before and after its own writes.
/// </summary>
public abstract class ReportTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected const string Reports = "reports";

    protected static DateOnly Today => BusinessDay.Today;

    protected static string Day(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    protected static string TodayOnly => $"from={Day(Today)}&to={Day(Today)}";

    protected override string GetUrl() => Reports;

    protected override string GetUrl(int id) => $"{Reports}/{id}";

    protected Task<TReport> GetReportAsync<TReport>(string report, string query) =>
        _client.GetAsync<TReport>($"{Reports}/{report}?{query}");

    protected async Task<int> CreateWarehouseAsync(bool archived = false)
    {
        var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent", IsArchived = archived };
        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync();

        return warehouse.Id;
    }

    protected async Task<int> CreateProductAsync(decimal salePrice = 100m, bool archived = false)
    {
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            SalePrice = salePrice,
            SupplyPrice = 50m,
            RetailPrice = salePrice,
            IsArchived = archived,
            Measurement = DomainUnit.Piece,
            Type = DomainProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }

    protected async Task<int> CreatePartnerAsync()
    {
        var partner = new Partner { Name = $"Partner {Guid.NewGuid():N}", Type = DomainPartnerType.Both };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    protected async Task<int> CreateWalletAsync(decimal openingBalance = 1_000_000m, DateTimeOffset? createdAt = null)
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = DomainWalletType.Cash,
            OpeningBalance = openingBalance,
            CreatedAt = createdAt ?? DateTimeOffset.UtcNow,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return wallet.Id;
    }

    protected async Task<int> CreateEmployeeAsync()
    {
        var employee = new Employee
        {
            FullName = $"Employee {Guid.NewGuid():N}",
            Position = "Cashier",
            Salary = 1_000m,
            Status = DomainEmployeeStatus.Active,
            DateOfEmployment = new DateOnly(2026, 1, 1),
        };
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return employee.Id;
    }

    protected Task AddOpeningStockAsync(int warehouseId, int productId, decimal quantity, decimal unitCost) =>
        _client.PostAsync<WarehouseDto>(
            $"warehouses/{warehouseId}/opening-stock",
            new { warehouseId, items = new[] { new { productId, quantity, unitCost } } },
            HttpStatusCode.OK);

    protected Task<StockAdjustmentDto> AdjustAsync(int warehouseId, int productId, string direction, decimal quantity, string reason) =>
        _client.PostAsync<StockAdjustmentDto>(
            "stock-adjustments",
            new { warehouseId, productId, direction, quantity, reason, note = (string?)null });

    protected Task<TransactionDto> PostTransactionAsync(CreateTransactionRequest request) =>
        _client.PostAsync<TransactionDto>("transactions", request.ToMultipartFormData(), HttpStatusCode.Created);

    /// <summary>A one-line document; a refund names its original and is priced from it.</summary>
    protected static CreateTransactionRequest Document(
        TransactionType type,
        int partnerId,
        int productId,
        int warehouseId,
        decimal quantity,
        decimal unitPrice,
        decimal discount = 0m,
        int? originalId = null,
        int? walletId = null,
        decimal paid = 0m)
        => new(
            PartnerId: partnerId,
            Type: type,
            Notes: null,
            Lines: [new CreateTransactionLine(productId, unitPrice, discount, DiscountType.Percentage, quantity)],
            WalletId: walletId,
            PaidAmount: paid,
            Settlements: null,
            Overpayment: OverpaymentHandling.Change,
            Attachments: null!,
            WarehouseId: warehouseId,
            OriginalTransactionId: originalId,
            RefundReason: originalId is null ? null : "Returned");
}
