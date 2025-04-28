using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class GetCategoriesTests : CategoryTestsBase
{
    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetCategoriesRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoCategories()
    {
        // Arrange
        var request = new GetCategoriesRequest(string.Empty);
        SetupCategories([]);

        // Act
        var result = await _service.GetAsync(request);

        // Assert
        Assert.Empty(result);
    }

    [Theory, MemberData(nameof(EmptyRequests))]
    public async Task GetAsync_ShouldReturnAll_WhenNoSearchTermProvided(GetCategoriesRequest request)
    {
        // Arrange

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(_defaultCategories.Length, response.Length);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnMatchingCategories_WhenSearchTermIsProvided()
    {
        // Arrange
        var request = new GetCategoriesRequest("Test Match");
        var matchingCategories = CreateMatchingCategories(request);
        SetupCategories([.. _defaultCategories, .. matchingCategories]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(matchingCategories.Length, response.Length);
        Assert.All(response, actual =>
        {
            var expected = matchingCategories.SingleOrDefault(x => x.Id == actual.Id);

            Assert.NotNull(expected);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Description, actual.Description);
        });
    }

    private Category[] CreateMatchingCategories(GetCategoriesRequest request)
    {
        var matchingName = _builder.CategoryBuilder
            .WithId(100)
            .WithName(request.SearchTerm)
            .BuildAndPopulate();
        var matchingDescription = _builder.CategoryBuilder
            .WithId(101)
            .WithDescription(request.SearchTerm)
            .BuildAndPopulate();

        return [matchingName, matchingDescription];
    }
}
