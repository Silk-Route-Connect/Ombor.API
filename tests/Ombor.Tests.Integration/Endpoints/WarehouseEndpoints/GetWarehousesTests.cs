using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.WarehouseEndpoints;

public class GetWarehousesTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : WarehouseTestsBase(factory, outputHelper)
{
    private const string _matchingSerachTerm = "Warehouse abs";
    public static TheoryData<GetWarehousesRequest> Requests =>
        new()
        {
            new GetWarehousesRequest(),
            new GetWarehousesRequest("   "),
            new GetWarehousesRequest("Test warehouse"),
            new GetWarehousesRequest(null),
        };

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task GetAsync_ShouldReturnMatchingWarehouses(GetWarehousesRequest request)
    {
        // Arrange
        await CreateWarehousesAsync(request);
        var url = GetUrl(request);

        // Act
        var response = await _client.GetAsync<WarehouseDto[]>(url);

        // Assert
        await _responseValidator.Warehouse.ValidateGetAsync(request, response);
    }

    [Fact]
    public async Task GetAsync_ShouldIncludeArchivedWarehouses()
    {
        // Arrange — an archived warehouse must still appear in the list (rule 31).
        var archived = await CreateWarehouseAsync(new Warehouse
        {
            Name = $"Archived {Guid.NewGuid():N}",
            Location = "Tashkent",
            IsArchived = true,
        });

        // Act
        var response = await _client.GetAsync<WarehouseDto[]>(GetUrl());

        // Assert
        var actual = Assert.Single(response, x => x.Id == archived);
        Assert.True(actual.IsArchived);
    }

    private async Task CreateWarehousesAsync(GetWarehousesRequest request)
    {
        var searchTerm = request.SearchTerm ?? _matchingSerachTerm;

        var warehouses = new List<Warehouse>
        {
            new()
            {
                Name=searchTerm,
                Location="Tashkent",
            },

            new()
            {
                Name="warehouse1",
                Location=searchTerm,
            },

            new()
            {
                Name=string.Empty,
                Location="12345",
            },

            new()
            {
                Name=searchTerm,
                Location="@@@@",
                IsArchived=true,
            }
        };
        _context.Warehouses.AddRange(warehouses);
        await _context.SaveChangesAsync();
    }
}
