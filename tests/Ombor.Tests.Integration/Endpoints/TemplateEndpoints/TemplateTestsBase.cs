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
