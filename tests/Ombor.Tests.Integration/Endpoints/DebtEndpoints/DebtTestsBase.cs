using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Responses.Debt;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.DebtEndpoints;

public abstract class DebtTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Debt;
    protected override string GetUrl(int id) => $"{Routes.Debt}/{id}";

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

    protected async Task<int> CreateWalletAsync()
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = 100_000_000m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return wallet.Id;
    }

    protected Task<int> CreatePartnerAsync() => AddPartnerAsync(_context);

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

    /// <summary>Adds a partner directly (no HTTP) to the given context's organization.</summary>
    protected static async Task<int> AddPartnerAsync(IApplicationDbContext context, string company = "Acme LLC")
    {
        var partner = new Partner
        {
            Name = $"Partner {Guid.NewGuid():N}",
            CompanyName = company,
            Type = PartnerType.Both,
            PhoneNumbers = ["+998900000000"],
        };
        context.Partners.Add(partner);
        await context.SaveChangesAsync();

        return partner.Id;
    }

    /// <summary>
    /// Seeds a transaction row directly (no HTTP) so its date can be back-dated for age/overdue
    /// assertions and so a row can be planted in another organization for scoping tests.
    /// </summary>
    protected static async Task<int> AddOutstandingTransactionAsync(
        IApplicationDbContext context,
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

        // Every transaction carries a required warehouse FK; reuse one in this context's organization or
        // create a throwaway (this also runs against another organization's context for scoping tests).
        var warehouseId = await context.Warehouses.Select(w => w.Id).FirstOrDefaultAsync();
        if (warehouseId == 0)
        {
            var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent" };
            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();
            warehouseId = warehouse.Id;
        }

        var transaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = type,
            WarehouseId = warehouseId,
            Status = status,
            TotalDue = totalDue,
            TotalPaid = totalPaid,
            DateUtc = dateUtc,
            DueDate = dueDate,
        };
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        return transaction.Id;
    }

    /// <summary>A context bound to a specific organization, for cross-organization isolation tests.</summary>
    private protected ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
