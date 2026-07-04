using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TemplateEndpoints;

public abstract class TemplateTestsBase(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : EndpointTestsBase(factory, outputHelper)
{
    protected readonly string _searchTerm = "Test Search";

    protected override string GetUrl()
    => Routes.Template;

    protected override string GetUrl(int id)
        => $"{Routes.Template}/{id}";

    protected async Task<Template> CreateTemplateAsync(Partner partner)
    {
        var template = new Template
        {
            Name = "Test Template",
            Type = TemplateType.Sale,
            PartnerId = partner.Id,
            Partner = partner,
            Items = GetItems(),
        };

        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        return template;
    }

    protected async Task<int> CreateAsync(Template template)
    {
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();

        return template.Id;
    }

    protected async Task<int[]> CreateAsync(Template[] templates)
    {
        _context.Templates.AddRange(templates);
        await _context.SaveChangesAsync();

        return [.. templates.Select(x => x.Id)];
    }

    protected async Task<Partner> CreatePartnerAsync(string name)
    {
        var partner = _builder.PartnerBuilder
            .WithName(name)
            .WithType(PartnerType.Both)
            .Build();

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner;
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
            Measurement = UnitOfMeasurement.Kilogram,
            Type = ProductType.All,
            CategoryId = category.Id,
            Category = null!,
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product.Id;
    }

    private static List<TemplateItem> GetItems() =>
        Enumerable.Range(1, 5)
        .Select(i => new TemplateItem
        {
            Quantity = i,
            UnitPrice = i * 1000,
            DiscountAmount = 0,
            ProductId = i,
            Product = null!,
            Template = null!,
        })
        .ToList();
}
