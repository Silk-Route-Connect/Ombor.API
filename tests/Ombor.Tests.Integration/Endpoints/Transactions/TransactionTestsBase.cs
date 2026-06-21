using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Transactions;

public abstract class TransactionsTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper output)
        : EndpointTestsBase(factory, output)
{
    protected override string GetUrl() => Routes.Transaction;
    protected override string GetUrl(int id) => $"{Routes.Transaction}/{id}";

    protected async Task<int> CreatePartnerAsync()
    {
        var partner = new Partner
        {
            Name = _faker.Person.FullName,
            Type = PartnerType.Both
        };
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    protected async Task<int> CreateWalletAsync(decimal openingBalance = 0m)
    {
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = WalletType.Cash,
            OpeningBalance = openingBalance,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        return wallet.Id;
    }

    protected async Task<int> CreateWarehouseAsync()
    {
        var warehouse = new Warehouse
        {
            Name = $"Warehouse {Guid.NewGuid():N}",
            Location = "Tashkent",
        };
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

    /// <summary>Seeds an warehouse item so a Sale (stock-out) has stock to draw from.</summary>
    protected async Task SeedStockAsync(int warehouseId, int productId, int quantity, decimal averageCost = 50m)
    {
        var item = new WarehouseItem
        {
            WarehouseId = warehouseId,
            ProductId = productId,
            Quantity = quantity,
            AverageCost = averageCost,
            Warehouse = null!,
            Product = null!,
        };
        _context.WarehouseItems.Add(item);
        await _context.SaveChangesAsync();
    }

    protected async Task<int> CreateOpenTransactionAsync(int partnerId, decimal due, decimal paid, TransactionType type = TransactionType.Sale)
    {
        var openTransaction = new TransactionRecord
        {
            PartnerId = partnerId,
            Partner = null!,
            Type = type,
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = due,
            TotalPaid = paid,
            Status = TransactionStatus.PartiallyPaid,
        };
        _context.Transactions.Add(openTransaction);
        await _context.SaveChangesAsync();

        return openTransaction.Id;
    }

    /// <summary>
    /// Seeds a partner advance on the new model: a payment with a wallet source and an
    /// <see cref="PaymentAllocationType.AdvanceCredit"/> allocation. Income → partner prepaid us
    /// (PartnerAdvance); Expense → we prepaid the partner (CompanyAdvance).
    /// </summary>
    protected async Task<int> CreateAdvancePaymentAsync(int partnerId, decimal amount, PaymentDirection direction = PaymentDirection.Income)
    {
        var walletId = await CreateWalletAsync();

        var payment = new Payment
        {
            DateUtc = DateTimeOffset.UtcNow,
            Notes = "Advance payment for test",
            Type = PaymentType.General,
            Direction = direction,
            PartnerId = partnerId,
            WalletId = walletId,
        };
        payment.Components.Add(new PaymentComponent
        {
            Amount = amount,
            SourceType = PaymentSourceType.Wallet,
            WalletId = walletId,
            Payment = null!,
        });
        payment.Allocations.Add(new PaymentAllocation
        {
            Amount = amount,
            Type = PaymentAllocationType.AdvanceCredit,
            Payment = null!,
        });

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return payment.Id;
    }
}
