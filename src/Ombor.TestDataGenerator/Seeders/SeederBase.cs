using System.Reflection;
using Bogus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Configurations;
using Ombor.Application.Helpers;
using Ombor.Application.Interfaces;
using Ombor.Application.Interfaces.File;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.TestDataGenerator.Configurations;
using Ombor.TestDataGenerator.Generators;

namespace Ombor.TestDataGenerator.Seeders;

internal abstract class SeederBase(
    DataSeedSettings seedSettings,
    FileSettings fileSettings,
    IWebHostEnvironment env,
    IImageThumbnailer thumbnailer,
    IPasswordHasher passwordHasher)
{
    private const string imagesNamespace = "Ombor.TestDataGenerator.Resources.Images.";
    private readonly Assembly currentAssembly = typeof(ProductGenerator).Assembly;

    protected static readonly Random _random = new();
    protected readonly FileSettings fileSettings = fileSettings;
    private readonly Faker _faker = new(seedSettings.Locale);

    /// <summary>
    /// Ensures <see cref="DataSeedSettings.NumberOfOrganizations"/> organizations exist, each with a
    /// loginable owner <see cref="User"/> and <see cref="Role"/>. Organizations, users and roles
    /// are not <c>IOrganizationScoped</c>, so they are created with no organization pinned (the global
    /// query filter and insert stamping are bypassed). Returns the organization ids to seed.
    /// </summary>
    protected async Task<int[]> EnsureOrganizationsWithUsersAsync(IApplicationDbContext context)
    {
        var organizations = context.Organizations
            .OrderBy(t => t.Id)
            .ToList();

        for (var index = organizations.Count + 1; index <= seedSettings.NumberOfOrganizations; index++)
        {
            var organization = new Organization
            {
                Name = $"Demo Organization {index}",
                IsActive = true,
            };
            context.Organizations.Add(organization);
            await context.SaveChangesAsync(); // need organization.Id for the role/user FKs

            var role = new Role
            {
                Name = "Owner",
                Description = "Seeded owner role.",
                OrganizationId = organization.Id,
                Organization = null! // set by EF Core via OrganizationId
            };

            var password = passwordHasher.HashPassword(seedSettings.SeedUserPassword);
            var user = new User
            {
                FirstName = "Demo",
                LastName = $"User {index}",
                PhoneNumber = $"+9989000000{index:00}",
                PasswordHash = password.Hash,
                PasswordSalt = password.Salt,
                IsPhoneNumberConfirmed = true,
                OrganizationId = organization.Id,
                Organization = null! // set by EF Core via OrganizationId
            };
            user.Roles.Add(role);

            context.Roles.Add(role);
            context.Users.Add(user);
            await context.SaveChangesAsync();

            organizations.Add(organization);
        }

        return organizations
            .Take(seedSettings.NumberOfOrganizations)
            .Select(t => t.Id)
            .ToArray();
    }

    protected Task<Dictionary<string, string>> EnsureImagesCopiedAsync()
    {
        var originalsDirectory = Path.Combine(
            env.WebRootPath,
            fileSettings.BasePath,
            fileSettings.ProductUploadsSection,
            fileSettings.OriginalsSubfolder);
        var thumbsDirectory = Path.Combine(
            env.WebRootPath,
            fileSettings.BasePath,
            fileSettings.ProductUploadsSection,
            fileSettings.ThumbnailsSubfolder);

        if (Directory.Exists(originalsDirectory))
        {
            Directory.Delete(originalsDirectory, true);
        }

        if (Directory.Exists(thumbsDirectory))
        {
            Directory.Delete(thumbsDirectory, true);
        }

        Directory.CreateDirectory(originalsDirectory);
        Directory.CreateDirectory(thumbsDirectory);

        return ExtractAndSaveImagesAsync(originalsDirectory, thumbsDirectory);
    }

    private async Task<Dictionary<string, string>> ExtractAndSaveImagesAsync(string originalsDir, string thumbsDir)
    {
        var nameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var resourceNames = currentAssembly.GetManifestResourceNames()
                     .Where(n => n.StartsWith(imagesNamespace, StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            var originalFileName = resourceName[imagesNamespace.Length..];
            var extension = Path.GetExtension(originalFileName);
            var storageFileName = $"{Guid.NewGuid():N}{extension}";

            nameMap[storageFileName] = originalFileName;

            // copy original
            await using var originalImageStream = currentAssembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException(resourceName);
            await using var originalImageFileStream = File.Create(Path.Combine(originalsDir, storageFileName));
            await originalImageStream.CopyToAsync(originalImageFileStream);

            // generate & save thumbnail
            originalImageStream.Position = 0;
            var format = ImageHelper.GetThumbnailFormat(extension);
            await using var thumbnailStream = await thumbnailer.GenerateThumbnailAsync(originalImageFileStream, format);
            await using var thumbnailImageFileStream = File.Create(Path.Combine(thumbsDir, storageFileName));
            await thumbnailStream.CopyToAsync(thumbnailImageFileStream);
        }

        return nameMap;
    }

    private const int MaxLinesPerTransaction = 5;
    private const int MaxSaleQuantity = 20;
    private const int MinSupplyQuantity = 20;
    private const int MaxSupplyQuantity = 200;
    private const int OpeningStockMin = 200;
    private const int OpeningStockMax = 1_000;
    private const double RefundChance = 0.3;

    private static readonly string[] RefundReasons =
    [
        "Брак товара",
        "Возврат по требованию клиента",
        "Пересорт при отгрузке",
        "Истёк срок годности",
        "Повреждение при доставке",
    ];

    /// <summary>
    /// Ensures the current organization has warehouses with opening stock. Opening stock is a distinct set
    /// of products per warehouse — no duplicate warehouse+product rows, which the stock read path keys by
    /// product and cannot hold — carried at a realistic cost so later stock-outs leave at a meaningful WAC.
    /// </summary>
    protected async Task EnsureWarehousesAsync(IApplicationDbContext context)
    {
        if (context.Warehouses.Any())
        {
            return;
        }

        var products = context.Products.ToArray();
        var prefix = seedSettings.Locale == "ru" ? "Склад" : "Warehouse";
        var warehouses = new List<Warehouse>();

        for (var index = 1; index <= seedSettings.NumberOfWarehouses; index++)
        {
            var warehouse = new Warehouse
            {
                Name = $"{prefix} {index}",
                Location = _faker.Address.StreetAddress(),
            };

            foreach (var product in PickDistinctProducts(products, seedSettings.NumberOfItemsPerWarehouse))
            {
                warehouse.WarehouseItems.Add(new WarehouseItem
                {
                    ProductId = product.Id,
                    Quantity = _random.Next(OpeningStockMin, OpeningStockMax + 1),
                    AverageCost = product.SupplyPrice > 0m ? product.SupplyPrice : product.SalePrice,
                    Product = null!,
                    Warehouse = null!,
                });
            }

            warehouses.Add(warehouse);
        }

        context.Warehouses.AddRange(warehouses);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds Supply, Sale, SaleRefund and SupplyRefund transactions for the current organization as data the
    /// API itself would accept: each is pinned to a real warehouse and actually moves stock (WAC recomputed on
    /// a weighted-average stock-in per rule 18; stock-outs hard-blocked below zero per rule 20), and each refund
    /// references a real non-refund original of the matching type (rules 2-4), carries a reason (rule 7) and
    /// stays within the original line quantity (rules 5-6).
    ///
    /// The invariants are replicated here rather than driven through <c>TransactionService</c>: the service
    /// hard-blocks any stock-out below zero, so pushing random bulk stock-outs through it would fail
    /// nondeterministically. The caller has to plan quantities against live stock either way — which is what
    /// this routine does, decrementing an in-memory stock ledger as it generates.
    /// </summary>
    protected async Task SeedTransactionsAsync(IApplicationDbContext context)
    {
        if (context.Transactions.Any())
        {
            return;
        }

        var warehouseIds = context.Warehouses.Select(w => w.Id).ToArray();
        if (warehouseIds.Length == 0)
        {
            return;
        }

        var products = context.Products.ToArray();
        var partners = context.Partners.ToArray();
        var stock = await LoadStockAsync(context, warehouseIds);

        // Supplies first so sales have stock to draw from; refunds then reference the originals just created.
        var supplies = SeedSupplies(context, partners, products, warehouseIds, stock);
        await context.SaveChangesAsync();

        var sales = SeedSales(context, partners, products, warehouseIds, stock);
        await context.SaveChangesAsync();

        SeedSaleRefunds(context, sales, stock);
        SeedSupplyRefunds(context, supplies, stock);
        await context.SaveChangesAsync();
    }

    private static async Task<Dictionary<(int WarehouseId, int ProductId), WarehouseItem>> LoadStockAsync(
        IApplicationDbContext context, int[] warehouseIds)
    {
        var items = await context.WarehouseItems
            .Where(i => warehouseIds.Contains(i.WarehouseId))
            .ToListAsync();

        return items.ToDictionary(i => (i.WarehouseId, i.ProductId));
    }

    private List<TransactionRecord> SeedSupplies(
        IApplicationDbContext context,
        Partner[] partners,
        Product[] products,
        int[] warehouseIds,
        Dictionary<(int, int), WarehouseItem> stock)
    {
        var supplyPartners = partners.Where(p => p.Type != PartnerType.Customer).ToArray();
        var supplyProducts = products.Where(p => p.Type != ProductType.Sale).ToArray();
        var supplies = new List<TransactionRecord>();

        if (supplyPartners.Length == 0 || supplyProducts.Length == 0)
        {
            return supplies;
        }

        foreach (var partner in supplyPartners)
        {
            var count = _random.Next(1, seedSettings.NumberOfMaxTransactionsPerPartner + 1);
            for (var i = 0; i < count; i++)
            {
                var warehouseId = warehouseIds[_random.Next(warehouseIds.Length)];
                var lines = new List<TransactionLine>();

                foreach (var product in PickDistinctProducts(supplyProducts, _random.Next(1, MaxLinesPerTransaction + 1)))
                {
                    var quantity = _random.Next(MinSupplyQuantity, MaxSupplyQuantity + 1);
                    var unitPrice = UnitPriceFor(product, isSale: false);
                    ApplyStockIn(context, stock, warehouseId, product.Id, quantity, unitPrice, recomputeWac: true);
                    lines.Add(NewLine(product.Id, quantity, unitPrice));
                }

                if (lines.Count == 0)
                {
                    continue;
                }

                var supply = NewTransaction(partner.Id, warehouseId, TransactionType.Supply, lines);
                context.Transactions.Add(supply);
                supplies.Add(supply);
            }
        }

        return supplies;
    }

    private List<TransactionRecord> SeedSales(
        IApplicationDbContext context,
        Partner[] partners,
        Product[] products,
        int[] warehouseIds,
        Dictionary<(int, int), WarehouseItem> stock)
    {
        var salePartners = partners.Where(p => p.Type != PartnerType.Supplier).ToArray();
        var saleProducts = products.Where(p => p.Type != ProductType.Supply).ToArray();
        var sales = new List<TransactionRecord>();

        if (salePartners.Length == 0 || saleProducts.Length == 0)
        {
            return sales;
        }

        foreach (var partner in salePartners)
        {
            var count = _random.Next(1, seedSettings.NumberOfMaxTransactionsPerPartner + 1);
            for (var i = 0; i < count; i++)
            {
                var warehouseId = warehouseIds[_random.Next(warehouseIds.Length)];
                var lines = new List<TransactionLine>();

                foreach (var product in PickDistinctProducts(saleProducts, _random.Next(1, MaxLinesPerTransaction + 1)))
                {
                    // Stock-out clamped to what's on hand — rule 20 forbids going negative.
                    var moved = TakeStock(stock, warehouseId, product.Id, _random.Next(1, MaxSaleQuantity + 1));
                    if (moved <= 0)
                    {
                        continue;
                    }

                    lines.Add(NewLine(product.Id, moved, UnitPriceFor(product, isSale: true)));
                }

                if (lines.Count == 0)
                {
                    continue;
                }

                var sale = NewTransaction(partner.Id, warehouseId, TransactionType.Sale, lines);
                context.Transactions.Add(sale);
                sales.Add(sale);
            }
        }

        return sales;
    }

    private void SeedSaleRefunds(
        IApplicationDbContext context,
        List<TransactionRecord> sales,
        Dictionary<(int, int), WarehouseItem> stock)
    {
        foreach (var sale in sales)
        {
            if (sale.Lines.Count == 0 || _random.NextDouble() >= RefundChance)
            {
                continue;
            }

            var refundLines = new List<TransactionLine>();
            foreach (var line in sale.Lines)
            {
                var refundQuantity = _random.Next(1, (int)line.Quantity + 1); // never exceeds the original (rules 5-6)
                // Returned goods re-enter at the existing carrying cost (rule 18 stock-in, no WAC recompute).
                ApplyStockIn(context, stock, sale.WarehouseId, line.ProductId, refundQuantity, 0m, recomputeWac: false);
                refundLines.Add(NewLine(line.ProductId, refundQuantity, line.UnitPrice));
            }

            var refund = NewTransaction(sale.PartnerId, sale.WarehouseId, TransactionType.SaleRefund, refundLines);
            refund.OriginalTransactionId = sale.Id;
            refund.RefundReason = RandomRefundReason();
            context.Transactions.Add(refund);
        }
    }

    private void SeedSupplyRefunds(
        IApplicationDbContext context,
        List<TransactionRecord> supplies,
        Dictionary<(int, int), WarehouseItem> stock)
    {
        foreach (var supply in supplies)
        {
            if (supply.Lines.Count == 0 || _random.NextDouble() >= RefundChance)
            {
                continue;
            }

            var refundLines = new List<TransactionLine>();
            foreach (var line in supply.Lines)
            {
                // Returning supplied goods is a stock-out: clamp to on-hand (rule 20) and to the original (rules 5-6).
                var desired = _random.Next(1, (int)line.Quantity + 1);
                var moved = TakeStock(stock, supply.WarehouseId, line.ProductId, desired);
                if (moved <= 0)
                {
                    continue;
                }

                refundLines.Add(NewLine(line.ProductId, moved, line.UnitPrice));
            }

            if (refundLines.Count == 0)
            {
                continue;
            }

            var refund = NewTransaction(supply.PartnerId, supply.WarehouseId, TransactionType.SupplyRefund, refundLines);
            refund.OriginalTransactionId = supply.Id;
            refund.RefundReason = RandomRefundReason();
            context.Transactions.Add(refund);
        }
    }

    private static decimal UnitPriceFor(Product product, bool isSale)
    {
        var price = isSale ? product.SalePrice : product.SupplyPrice;
        return price > 0m ? price : 1_000m;
    }

    private static TransactionLine NewLine(int productId, decimal quantity, decimal unitPrice) => new()
    {
        ProductId = productId,
        Quantity = quantity,
        UnitPrice = unitPrice,
        Discount = 0m,
        DiscountType = DiscountType.Fixed,
        Product = null!,
        Transaction = null!,
    };

    private static TransactionRecord NewTransaction(
        int partnerId, int warehouseId, TransactionType type, List<TransactionLine> lines) => new()
        {
            PartnerId = partnerId,
            Partner = null!,
            WarehouseId = warehouseId,
            DateUtc = DateTimeOffset.UtcNow.AddDays(-_random.Next(0, 365)),
            Type = type,
            Status = TransactionStatus.Open,
            Lines = lines,
            TotalDue = lines.Sum(l => l.Total),
            TotalPaid = 0m,
        };

    private static string RandomRefundReason() => RefundReasons[_random.Next(RefundReasons.Length)];

    /// <summary>Weighted-average stock-in (rule 18) or carrying-cost re-entry; creates the row if absent.</summary>
    private static void ApplyStockIn(
        IApplicationDbContext context,
        Dictionary<(int, int), WarehouseItem> stock,
        int warehouseId,
        int productId,
        int quantity,
        decimal unitPrice,
        bool recomputeWac)
    {
        if (!stock.TryGetValue((warehouseId, productId), out var item))
        {
            item = new WarehouseItem
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                Quantity = 0,
                AverageCost = 0m,
                Product = null!,
                Warehouse = null!,
            };
            context.WarehouseItems.Add(item);
            stock[(warehouseId, productId)] = item;
        }

        if (recomputeWac)
        {
            var newQuantity = item.Quantity + quantity;
            item.AverageCost = newQuantity == 0
                ? 0m
                : ((item.Quantity * item.AverageCost) + (quantity * unitPrice)) / newQuantity;
            item.Quantity = newQuantity;
        }
        else
        {
            item.Quantity += quantity;
        }
    }

    /// <summary>Stock-out clamped to what's on hand so it never goes negative (rule 20). Returns units moved.</summary>
    private static decimal TakeStock(
        Dictionary<(int, int), WarehouseItem> stock, int warehouseId, int productId, decimal desiredQuantity)
    {
        if (!stock.TryGetValue((warehouseId, productId), out var item) || item.Quantity <= 0)
        {
            return 0;
        }

        var moved = Math.Min(desiredQuantity, item.Quantity);
        item.Quantity -= moved; // stock leaves at WAC (rule 19)
        return moved;
    }

    private static List<Product> PickDistinctProducts(Product[] products, int count)
    {
        if (products.Length <= count)
        {
            return [.. products];
        }

        var picked = new List<Product>(count);
        var seen = new HashSet<int>(count);

        for (var attempt = 0; picked.Count < count && attempt < count * 5; attempt++)
        {
            var product = products[_random.Next(products.Length)];
            if (seen.Add(product.Id))
            {
                picked.Add(product);
            }
        }

        return picked;
    }
}
