using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PartnerEndpoints;

public abstract class PartnerTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
        : EndpointTestsBase(factory, outputHelper)
{
    protected readonly string _searchTerm = "John";

    protected override string GetUrl()
        => Routes.partner;

    protected override string GetUrl(int id)
        => $"{Routes.partner}/{id}";

    protected async Task<int> CreatePartnerAsync()
    {
        var partner = new Partner
        {
            Name = "John",
            Address = "Tashkent",
            Email = "johnjohn123@gmail.com",
            CompanyName = "Company Name LLC",
            Type = PartnerType.Supplier,
            PhoneNumbers = ["+998974561100"]
        };

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }

    protected async Task<int> CreatePartnerAsync(Partner partner)
    {
        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return partner.Id;
    }
}
