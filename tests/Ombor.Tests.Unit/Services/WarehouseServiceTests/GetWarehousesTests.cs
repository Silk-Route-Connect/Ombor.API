using Moq;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.WarehouseServiceTests;

public sealed class GetWarehousesTests : WarehouseTestsBase
{
    private const string MatchingSearchTerm = "Test match";

    public static TheoryData<GetWarehousesRequest> GetRequests => new()
    {
        {new GetWarehousesRequest(null)},
        {new GetWarehousesRequest(string.Empty)},
        {new GetWarehousesRequest("qwerty")},
        {new GetWarehousesRequest("    ")},
        {new GetWarehousesRequest(MatchingSearchTerm)},
    };

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetWarehousesRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoWarehouses()
    {
        // Arrange
        var request = new GetWarehousesRequest(string.Empty);
        SetupWarehouses([]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Empty(response);

        _mockContext.Verify(mock => mock.Warehouses, Times.Once);

        VerifyNoOtherCalls();
    }

    [Theory, MemberData(nameof(GetRequests))]
    public async Task GetAsync_ShouldReturnMatchingWarehouses(GetWarehousesRequest request)
    {
        // Arrange
        var matchingWarehouses = CreatingMatchingWarehouses(request);
        Warehouse[] allWarehouses = [.. _defaultWarehouses, .. matchingWarehouses];
        var expectedWarehouses = request.IsEmpty()
            ? allWarehouses
            : matchingWarehouses;

        SetupWarehouses([.. _defaultWarehouses, .. matchingWarehouses]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(expectedWarehouses.Length, response.Length);
        Assert.All(response, actual =>
        {
            var expected = expectedWarehouses.SingleOrDefault(x => x.Id == actual.Id);

            WarehouseAssertionHelper.AssertEquivalent(expected, actual);
        });

        _mockContext.Verify(mock => mock.Warehouses, Times.Once);

        VerifyNoOtherCalls();
    }

    private Warehouse[] CreatingMatchingWarehouses(GetWarehousesRequest request)
    {
        if (request.IsEmpty())
        {
            return [];
        }

        var matchingName = _builder.WarehouseBuilder
            .WithId(100)
            .WithName(request.SearchTerm)
            .WithLocation("test location")
            .BuildAndPopulate();

        var matchingLocation = _builder.WarehouseBuilder
            .WithId(101)
            .WithName("test name")
            .WithLocation(request.SearchTerm)
            .BuildAndPopulate();

        return [matchingName, matchingLocation];
    }
}
