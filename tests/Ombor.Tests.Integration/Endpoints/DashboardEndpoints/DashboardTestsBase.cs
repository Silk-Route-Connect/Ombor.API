using System.Net;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Contracts.Responses.Debt;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.DashboardEndpoints;

public abstract class DashboardTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Dashboard;
    protected override string GetUrl(int id) => $"{Routes.Dashboard}/{id}";

    protected Task<DashboardDto> GetDashboardAsync(string? period = null) =>
        _client.GetAsync<DashboardDto>(period is null ? Routes.Dashboard : $"{Routes.Dashboard}?period={period}");

    protected Task<DebtDto[]> GetDebtsAsync() => _client.GetAsync<DebtDto[]>(Routes.Debt);

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
        var partner = new Partner
        {
            Name = $"Partner {Guid.NewGuid():N}",
            CompanyName = "Acme LLC",
            Type = PartnerType.Both,
            PhoneNumbers = ["+998900000000"],
        };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    protected async Task<int> CreateWalletAsync(WalletType type = WalletType.Cash)
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = type,
            OpeningBalance = 100_000_000m,
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

    protected async Task<TransactionDto> PostTransactionAsync(CreateTransactionRequest request)
    {
        var form = request.ToMultipartFormData();

        return await _client.PostAsync<TransactionDto>(Routes.Transaction, form, HttpStatusCode.Created);
    }

    /// <summary>Seeds an outstanding transaction directly (no HTTP) so its date can be back-dated.</summary>
    protected async Task<int> AddOutstandingTransactionAsync(
        int partnerId,
        TransactionType type,
        decimal totalDue,
        decimal totalPaid,
        DateTimeOffset dateUtc,
        DateOnly? dueDate)
    {
        var status = totalPaid <= 0m
            ? TransactionStatus.Open
            : totalPaid < totalDue ? TransactionStatus.PartiallyPaid : TransactionStatus.Closed;

        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = type,
            WarehouseId = await EnsureWarehouseAsync(),
            Status = status,
            TotalDue = totalDue,
            TotalPaid = totalPaid,
            DateUtc = dateUtc,
            DueDate = dueDate,
        };
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        return transaction.Id;
    }
}
