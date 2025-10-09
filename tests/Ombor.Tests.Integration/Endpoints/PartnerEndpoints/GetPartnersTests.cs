using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PartnerEndpoints;

public class GetPartnersTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
        : PartnerTestsBase(factory, outputHelper)
{
    private const string _matchingSerachTerm = "Partner Search";

    [Fact]
    public async Task GetAsync_ShouldReturnFilteredPartners_WhenSearchIsProvided()
    {
        // Arrange
        var request = new GetPartnersRequest(searchTerm: _searchTerm);
        await Createpartners(request);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<PartnerDto[]>(url);

        // Assert
        await _responseValidator.Partner.ValidateGetAsync(request, response);
    }

    private async Task Createpartners(GetPartnersRequest request)
    {
        var searchTerm = request.SearchTerm ?? _matchingSerachTerm;

        var partners = new List<Partner>
        {
            // Matching search term by name
            new()
            {
                Name = searchTerm,
                Address = "Address",
                Email = "search-test@gmail.com",
                CompanyName = "Partner's company name",
                Type = PartnerType.Customer,
                Balance = 1000.00m,
                PhoneNumbers = ["+998914778888"]
            },
            // Matching search term by address
            new()
            {
                Name = "Test Partner For Search",
                Address = searchTerm,
                Email = "search-test1@gmail.com",
                CompanyName = "Partner's company name",
                Type = PartnerType.Supplier,
                Balance = 1000.00m,
                PhoneNumbers = ["+998914778888"]
            },
            // Matching company
            new()
            {
                Name = "Test Partner for search matching Company",
                Address = "Tashkent",
                CompanyName = searchTerm,
                Type = PartnerType.Both,
                Balance = 10_000,
                PhoneNumbers = ["+99890-100-00-00"]
            },
        };

        _context.Partners.AddRange(partners);
        await _context.SaveChangesAsync();
    }
}
