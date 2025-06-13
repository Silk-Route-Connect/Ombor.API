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
    private const string _matchingSerachTerm = "Test Partner";

    [Fact]
    public async Task GetAsync_ShouldReturnFilteredpartners_WhenSearchIsProvided()
    {
        // Arrange
        var request = new GetpartnersRequest(_searchTerm);
        await Createpartners(request);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<PartnerDto[]>(url);

        // Assert
        await _responseValidator.partner.ValidateGetAsync(request, response);
    }

    private async Task Createpartners(GetpartnersRequest request)
    {
        var searchTerm = request.SearchTerm ?? _matchingSerachTerm;

        var partners = new List<Partner>
        {
            // Matching search term by name
            new()
            {
                Name = searchTerm,
                Address = "Address",
                Email = "partner's email",
                CompanyName = "partner's company name",
                Type = PartnerType.Customer,
                Balance = 1000.00m,
                PhoneNumbers = ["+998914778888"]
            },
            // Matching search term by address
            new()
            {
                Name = "partner's Name",
                Address = searchTerm,
                Email = "partner's email",
                CompanyName = "partner's company name",
                Type = PartnerType.Supplier,
                Balance = 1000.00m,
                PhoneNumbers = ["+998914778888"]
            }
        };

        _context.Partners.AddRange(partners);
        await _context.SaveChangesAsync();
    }
}
