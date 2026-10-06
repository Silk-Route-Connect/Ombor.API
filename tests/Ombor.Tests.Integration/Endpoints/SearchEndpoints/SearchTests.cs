using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Search;
using Ombor.Domain.Common;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using DomainEnums = Ombor.Domain.Enums;

namespace Ombor.Tests.Integration.Endpoints.SearchEndpoints;

/// <summary>
/// GET /api/search — the topbar search: one call over partners, products, documents by number, employees, warehouses
/// and wallets; Cyrillic and Latin find each other; archived records come back flagged; only this organization.
/// </summary>
public sealed class SearchTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    private const string Route = "search";
    private const int ForeignOrganizationId = 2;

    protected override string GetUrl() => Route;

    protected override string GetUrl(int id) => $"{Route}/{id}";

    [Theory]
    [InlineData("Холматов", "Xolmatov")]
    [InlineData("Холматов", "Kholmatov")]
    [InlineData("Жасур", "Jasur")]
    [InlineData("Жасур", "zhasur")]
    [InlineData("Ўзбекистон", "O‘zbekiston")]
    [InlineData("Ўзбекистон", "Ozbekiston")]
    [InlineData("Ғайрат", "Gʻayrat")]
    [InlineData("Шоколад", "SHOKOLAD")]
    [InlineData("O'zbekiston", "Ўзбекистон")]
    public async Task Partner_IsFoundAcrossScripts(string stored, string typed)
    {
        var token = Token();
        var partnerId = await AddPartnerAsync(_context, $"{stored} {token}");

        var result = await SearchAsync($"{typed} {token}");

        var hit = Assert.Single(result.Partners.Items);
        Assert.Equal(partnerId, hit.Id);
        Assert.Equal(ActivityEntityKind.Partner, hit.EntityKind);
        Assert.Equal(SearchMatch.Name, hit.MatchedOn);
        Assert.Equal(1, result.Partners.Total);
    }

    [Fact]
    public async Task Partner_IsFoundByCompany_AndByPhoneDigitsHoweverTyped()
    {
        var token = Token();
        var local = Digits7();
        var partnerId = await AddPartnerAsync(_context, $"Partner {token}", company: $"Firma {token}", phone: $"+99890{local}");

        var byCompany = await SearchAsync($"firma {token}");
        var byPhone = await SearchAsync($"90 {local[..3]} {local[3..5]} {local[5..]}");
        var byPart = await SearchAsync($"{local}");

        Assert.Equal(SearchMatch.Company, Assert.Single(byCompany.Partners.Items, h => h.Id == partnerId).MatchedOn);
        Assert.Equal(SearchMatch.Phone, Assert.Single(byPhone.Partners.Items, h => h.Id == partnerId).MatchedOn);
        Assert.Contains(byPart.Partners.Items, h => h.Id == partnerId);
    }

    [Fact]
    public async Task Product_IsFoundByNameSkuBarcodeAndPackageBarcode()
    {
        var token = Token();
        var barcode = $"47{Digits7()}{Digits7()[..4]}";
        var packageBarcode = $"48{Digits7()}{Digits7()[..4]}";
        var productId = await AddProductAsync($"Чай {token}", $"SKU-{token}", barcode, packageBarcode);

        var byName = await SearchAsync($"Chay {token}");
        var bySku = await SearchAsync($"sku-{token}");
        var byBarcode = await SearchAsync(barcode);
        var byPackage = await SearchAsync(packageBarcode);

        Assert.Equal(SearchMatch.Name, Assert.Single(byName.Products.Items).MatchedOn);
        var skuHit = Assert.Single(bySku.Products.Items);
        Assert.Equal(SearchMatch.Sku, skuHit.MatchedOn);
        Assert.Equal($"SKU-{token}", skuHit.Detail);
        Assert.Equal(SearchMatch.Barcode, Assert.Single(byBarcode.Products.Items, h => h.Id == productId).MatchedOn);
        Assert.Equal(SearchMatch.PackagingBarcode, Assert.Single(byPackage.Products.Items, h => h.Id == productId).MatchedOn);
    }

    [Fact]
    public async Task Documents_AreFoundByExactNumber_InEverySeries()
    {
        var number = Random.Shared.Next(800_000_000, 899_999_999);
        var partnerId = await AddPartnerAsync(_context, $"Partner {Token()}");
        var (saleId, paymentId, orderId) = await AddDocumentsAsync(_context, partnerId, number);

        var result = await SearchAsync($"№ {number}");
        var nearMiss = await SearchAsync($"{number + 1}");

        Assert.Equal(3, result.Documents.Total);
        var sale = Assert.Single(result.Documents.Items, h => h.EntityKind == ActivityEntityKind.Sale);
        Assert.Equal(saleId, sale.Id);
        Assert.Equal(number.ToString(), sale.Label);
        Assert.Equal(1_500m, sale.Amount);
        Assert.Equal(SearchMatch.Number, sale.MatchedOn);
        Assert.Equal(paymentId, Assert.Single(result.Documents.Items, h => h.EntityKind == ActivityEntityKind.Payment).Id);
        Assert.Equal(700m, Assert.Single(result.Documents.Items, h => h.EntityKind == ActivityEntityKind.Payment).Amount);
        Assert.Equal(orderId, Assert.Single(result.Documents.Items, h => h.EntityKind == ActivityEntityKind.Order).Id);
        Assert.DoesNotContain(nearMiss.Documents.Items, h => h.Id == saleId);
    }

    [Fact]
    public async Task EmployeesWarehousesAndWallets_AreFound_AndTerminatedEmployeesFlagged()
    {
        var token = Token();
        var local = Digits7();
        var employee = new Employee
        {
            FullName = $"Дилшод {token}",
            Position = "Кассир",
            Salary = 1_000m,
            Status = DomainEnums.EmployeeStatus.Terminated,
            DateOfEmployment = new DateOnly(2026, 1, 1),
            ContactInfo = new ContactInfo { PhoneNumbers = [$"+99891{local}"] },
        };
        var warehouse = new Warehouse { Name = $"Склад {token}", Location = "Chilonzor" };
        var wallet = new Wallet { Name = $"Касса {token}", Type = DomainEnums.WalletType.Cash, CreatedAt = DateTimeOffset.UtcNow };
        _context.Employees.Add(employee);
        _context.Warehouses.Add(warehouse);
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        var byName = await SearchAsync($"dilshod {token}");
        var byPhone = await SearchAsync($"91{local}");
        var warehouses = await SearchAsync($"sklad {token}");
        var wallets = await SearchAsync($"kassa {token}");

        var hit = Assert.Single(byName.Employees.Items);
        Assert.Equal(employee.Id, hit.Id);
        Assert.True(hit.IsArchived);
        Assert.Equal("Кассир", hit.Detail);
        Assert.Equal(SearchMatch.Phone, Assert.Single(byPhone.Employees.Items, h => h.Id == employee.Id).MatchedOn);
        Assert.Equal(warehouse.Id, Assert.Single(warehouses.Warehouses.Items).Id);
        Assert.Equal(wallet.Id, Assert.Single(wallets.Wallets.Items).Id);
    }

    [Fact]
    public async Task Hits_AreRankedExactPrefixWordContains_ActiveBeforeArchived_AndCappedByTheLimit()
    {
        var token = Token();
        var contains = await AddPartnerAsync(_context, $"x{token}");
        var word = await AddPartnerAsync(_context, $"Big {token}");
        var prefix = await AddPartnerAsync(_context, $"{token} Market");
        var exactArchived = await AddPartnerAsync(_context, token, archived: true);
        var exact = await AddPartnerAsync(_context, token.ToUpperInvariant());

        var all = await SearchAsync(token, limit: 10);
        var capped = await SearchAsync(token, limit: 2);

        Assert.Equal([exact, exactArchived, prefix, word, contains], all.Partners.Items.Select(h => h.Id));
        Assert.True(all.Partners.Items[1].IsArchived);
        Assert.Equal(5, capped.Partners.Total);
        Assert.Equal([exact, exactArchived], capped.Partners.Items.Select(h => h.Id));
    }

    [Fact]
    public async Task Search_ShowsOnlyThisOrganization()
    {
        var token = Token();
        var number = Random.Shared.Next(900_000_000, 999_999_999);
        await using (var foreign = CreateContext(ForeignOrganizationId))
        {
            var foreignPartner = await AddPartnerAsync(foreign, $"Partner {token}");
            await AddDocumentsAsync(foreign, foreignPartner, number);
        }

        var byName = await SearchAsync(token);
        var byNumber = await SearchAsync(number.ToString());

        Assert.Equal(0, byName.Partners.Total);
        Assert.Equal(0, byNumber.Documents.Total);
    }

    [Theory]
    [InlineData("q=")]
    [InlineData("q=%20%20")]
    [InlineData("limit=5")]
    [InlineData("q=abc&limit=0")]
    [InlineData("q=abc&limit=21")]
    public async Task InvalidQuery_IsBadRequest(string query)
    {
        await _client.GetAsync($"{Route}?{query}", HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TooLongQuery_IsBadRequest()
    {
        await _client.GetAsync($"{Route}?q={new string('a', 101)}", HttpStatusCode.BadRequest);
    }

    private Task<SearchResultsDto> SearchAsync(string query, int? limit = null) =>
        _client.GetAsync<SearchResultsDto>(
            $"{Route}?q={Uri.EscapeDataString(query)}{(limit is { } l ? $"&limit={l}" : string.Empty)}");

    // Hex only: no letter pair the skeleton folds (kh, zh, ts), so the token survives normalisation unchanged.
    private static string Token() => Guid.NewGuid().ToString("N")[..12];

    private static string Digits7() => Random.Shared.Next(1_000_000, 9_999_999).ToString();

    private static async Task<int> AddPartnerAsync(
        IApplicationDbContext context, string name, string? company = null, string? phone = null, bool archived = false)
    {
        var partner = new Partner
        {
            Name = name,
            CompanyName = company,
            Type = DomainEnums.PartnerType.Both,
            PhoneNumbers = phone is null ? [] : [phone],
            IsArchived = archived,
        };
        context.Partners.Add(partner);
        await context.SaveChangesAsync();

        return partner.Id;
    }

    private async Task<int> AddProductAsync(string name, string sku, string barcode, string packageBarcode)
    {
        var category = new Category { Name = $"Category {Guid.NewGuid():N}" };
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var product = new Product
        {
            Name = name,
            SKU = sku,
            Barcode = barcode,
            Packaging = new ProductPackaging { Size = 12, Label = "Блок", Barcode = packageBarcode },
            SalePrice = 100m,
            SupplyPrice = 50m,
            RetailPrice = 90m,
            Measurement = DomainEnums.UnitOfMeasurement.Piece,
            Type = DomainEnums.ProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }

    /// <summary>A sale, a payment and an order that all carry <paramref name="number"/> — one per numbering series.</summary>
    private static async Task<(int SaleId, int PaymentId, int OrderId)> AddDocumentsAsync(
        IApplicationDbContext context, int partnerId, int number)
    {
        var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent" };
        var wallet = new Wallet { Name = $"Wallet {Guid.NewGuid():N}", Type = DomainEnums.WalletType.Cash, CreatedAt = DateTimeOffset.UtcNow };
        context.Warehouses.Add(warehouse);
        context.Wallets.Add(wallet);
        await context.SaveChangesAsync();

        var sale = new TransactionRecord
        {
            Number = number,
            PartnerId = partnerId,
            Partner = null!,
            WarehouseId = warehouse.Id,
            Type = DomainEnums.TransactionType.Sale,
            Status = DomainEnums.TransactionStatus.Open,
            DateUtc = DateTimeOffset.UtcNow,
            TotalDue = 1_500m,
        };
        var payment = new Payment
        {
            Number = number,
            Type = DomainEnums.PaymentType.Deposit,
            Direction = DomainEnums.PaymentDirection.Income,
            DateUtc = DateTimeOffset.UtcNow,
            PartnerId = partnerId,
            WalletId = wallet.Id,
        };
        payment.Components.Add(new PaymentComponent { Amount = 700m, WalletId = wallet.Id, Payment = payment });
        var order = new Order
        {
            OrderNumber = number,
            CustomerId = partnerId,
            Customer = null!,
            TotalAmount = 2_000m,
            DateUtc = DateTimeOffset.UtcNow,
            Status = DomainEnums.OrderStatus.Pending,
        };
        context.Transactions.Add(sale);
        context.Payments.Add(payment);
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return (sale.Id, payment.Id, order.Id);
    }

    private ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
