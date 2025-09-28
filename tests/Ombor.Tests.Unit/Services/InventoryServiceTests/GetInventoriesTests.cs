using Moq;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.InventoryServiceTests;

public sealed class GetInventoriesTests : InventoryTestsBase
{
    private const string MatchingSearchTerm = "Test match";

    public static TheoryData<GetInventoriesRequest> GetRequests => new()
    {
        {new GetInventoriesRequest(null)},
        {new GetInventoriesRequest(string.Empty)},
        {new GetInventoriesRequest("qwerty")},
        {new GetInventoriesRequest("    ")},
        {new GetInventoriesRequest(MatchingSearchTerm)},
    };

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetInventoriesRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoInventories()
    {
        // Arrange
        var request = new GetInventoriesRequest(string.Empty);
        SetupInventories([]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Empty(response);

        _mockContext.Verify(mock => mock.Inventories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Theory, MemberData(nameof(GetRequests))]
    public async Task GetAsync_ShouldReturnMatchingInventories(GetInventoriesRequest request)
    {
        // Arrange
        var matchingInventories = CreatingMatchingInventories(request);
        Inventory[] allInventories = [.. _defaultInventories, .. matchingInventories];
        var expectedInventories = request.IsEmpty()
            ? allInventories
            : matchingInventories;

        SetupInventories([.. _defaultInventories, .. matchingInventories]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(expectedInventories.Length, response.Length);
        Assert.All(response, actual =>
        {
            var expected = expectedInventories.SingleOrDefault(x => x.Id == actual.Id);

            InventoryAssertionHelper.AssertEquivalent(expected, actual);
        });

        _mockContext.Verify(mock => mock.Inventories, Times.Once);

        VerifyNoOtherCalls();
    }

    private Inventory[] CreatingMatchingInventories(GetInventoriesRequest request)
    {
        if (request.IsEmpty())
        {
            return [];
        }

        var matchingName = _builder.InventoryBuilder
            .WithId(100)
            .WithName(request.SearchTerm)
            .WithLocation("test location")
            .BuildAndPopulate();

        var matchingLocation = _builder.InventoryBuilder
            .WithId(101)
            .WithName("test name")
            .WithLocation(request.SearchTerm)
            .BuildAndPopulate();

        return [matchingName, matchingLocation];
    }
}
