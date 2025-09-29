using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.InventoryEndpoints;

public class GetInventoriesTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : InventoryTestsBase(factory, outputHelper)
{
    private const string _matchingSerachTerm = "Inventory abs";
    public static TheoryData<GetInventoriesRequest> Requests =>
        new()
        {
            new GetInventoriesRequest(),
            new GetInventoriesRequest("   "),
            new GetInventoriesRequest("Test inventory"),
            new GetInventoriesRequest(null),
        };

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task GetAsync_ShouldReturnMatchingInventories(GetInventoriesRequest request)
    {
        // Arrange
        await CreateInventoriesAsync(request);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<InventoryDto[]>(url);

        // Assert
        await _responseValidator.Inventory.ValidateGetAsync(request, response);
    }

    private async Task CreateInventoriesAsync(GetInventoriesRequest request)
    {
        var searchTerm = request.SearchTerm ?? _matchingSerachTerm;

        var inventories = new List<Inventory>
        {
            new()
            {
                Name=searchTerm,
                Location="Tashkent",
                IsActive=true,
            },

            new()
            {
                Name="inventory1",
                Location=searchTerm,
                IsActive=true,
            },

            new()
            {
                Name=string.Empty,
                Location="12345",
                IsActive=true,
            },

            new()
            {
                Name=searchTerm,
                Location="@@@@",
                IsActive=false,
            }
        };
        _context.Inventories.AddRange(inventories);
        await _context.SaveChangesAsync();
    }
}
