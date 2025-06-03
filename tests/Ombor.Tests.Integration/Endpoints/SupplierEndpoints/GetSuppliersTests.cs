using Ombor.Contracts.Requests.Supplier;
using Ombor.Contracts.Responses.Supplier;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Endpoints.ProductEndpoints;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.SupplierEndpoints;

public class GetSuppliersTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
        : SupplierTestsBase(factory, outputHelper)
{
    private const string _matchingSerachTerm = "Test Supplier";

    [Fact]
    public async Task GetAsync_ShouldReturnFilteredSuppliers_WhenSearchIsProvided()
    {
        // Arrange
        var request = new GetSuppliersRequest(_searchTerm);
        await CreateSuppliers(request);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<SupplierDto[]>(url);

        // Assert
        await _responseValidator.Supplier.ValidateGetAsync(request, response);
    }

    private async Task CreateSuppliers(GetSuppliersRequest request)
    {
        var searchTerm = request.SearchTerm ?? _matchingSerachTerm;

        var suppliers = new List<Supplier>
        {
            // Matching search term by name
            new()
            {
                Name=searchTerm,
                Address="Address",
                Email="Supplier's email",
                CompanyName="Supplier's company name",
                IsActive=true,
                PhoneNumbers=["+998914778888"]
            },
            // Matching search term by address
            new()
            {
                Name="Supplier's Name",
                Address=searchTerm,
                Email="Supplier's email",
                CompanyName="Supplier's company name",
                IsActive=true,
                PhoneNumbers=["+998914778888"]
            }
        };

        _context.Suppliers.AddRange(suppliers);
        await _context.SaveChangesAsync();
    }
}
