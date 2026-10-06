using Ombor.Domain.Entities;
using Ombor.Infrastructure.Persistence;
using DomainEnums = Ombor.Domain.Enums;

namespace Ombor.Tests.Integration.Endpoints.OrganizationScoping;

/// <summary>One of each record a write can reference, created in whichever organization the context is pinned to.</summary>
internal sealed record TenantGraph(int PartnerId, int ProductId, int WarehouseId, int WalletId, int EmployeeId, int TransactionId)
{
    public static async Task<TenantGraph> CreateAsync(ApplicationDbContext context, bool withStock)
    {
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        var partner = new Partner { Name = $"Partner {Guid.NewGuid():N}", Type = DomainEnums.PartnerType.Both };
        var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent" };
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = DomainEnums.WalletType.Cash,
            OpeningBalance = 100_000m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var employee = new Employee
        {
            FullName = $"Employee {Guid.NewGuid():N}",
            Position = "Cashier",
            Salary = 1_000m,
            Status = DomainEnums.EmployeeStatus.Active,
            DateOfEmployment = new DateOnly(2026, 1, 1),
        };
        context.AddRange(category, partner, warehouse, wallet, employee);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = $"Product {Guid.NewGuid():N}",
            SKU = $"SKU-{Guid.NewGuid():N}",
            SalePrice = 1_000m,
            SupplyPrice = 500m,
            RetailPrice = 900m,
            Measurement = DomainEnums.UnitOfMeasurement.Piece,
            Type = DomainEnums.ProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        if (withStock)
        {
            context.WarehouseItems.Add(new WarehouseItem
            {
                WarehouseId = warehouse.Id,
                ProductId = product.Id,
                Quantity = 100m,
                AverageCost = 500m,
                Warehouse = null!,
                Product = null!,
            });
        }

        var transaction = new TransactionRecord
        {
            PartnerId = partner.Id,
            Partner = null!,
            WarehouseId = warehouse.Id,
            Type = DomainEnums.TransactionType.Sale,
            Status = DomainEnums.TransactionStatus.Open,
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = 5_000m,
            TotalPaid = 0m,
        };
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        return new TenantGraph(partner.Id, product.Id, warehouse.Id, wallet.Id, employee.Id, transaction.Id);
    }
}
