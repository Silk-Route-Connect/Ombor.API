using Moq;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class GetCategoriesTests : CategoryTestsBase
{
    private const string MatchingSearchTerm = "Test Match";

    public static TheoryData<GetCategoriesRequest> GetRequests => new()
    {
        { new GetCategoriesRequest(null) },
        { new GetCategoriesRequest(string.Empty) },
        { new GetCategoriesRequest(" ") },
        { new GetCategoriesRequest("   ") },
        { new GetCategoriesRequest(MatchingSearchTerm) },
    };

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetCategoriesRequest request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoCategories()
    {
        // Arrange
        var request = new GetCategoriesRequest(string.Empty);
        SetupCategories([]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Empty(response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Categories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Theory, MemberData(nameof(GetRequests))]
    public async Task GetAsync_ShouldReturnMatchingCategories(GetCategoriesRequest request)
    {
        // Arrange
        var matchingCategories = CreateMatchingCategories(request);
        Category[] allCategories = [.. _defaultCategories, .. matchingCategories];
        var expectedCategories = request.IsEmpty()
            ? allCategories
            : matchingCategories;

        SetupCategories([.. _defaultCategories, .. matchingCategories]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(expectedCategories.Length, response.Count);
        Assert.All(response, actual =>
        {
            var expected = expectedCategories.SingleOrDefault(x => x.Id == actual.Id);

            CategoryAssertionHelper.AssertEquivalent(expected, actual);
        });

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Categories, Times.Once);

        VerifyNoOtherCalls();
    }

    private Category[] CreateMatchingCategories(GetCategoriesRequest request)
    {
        if (request.IsEmpty())
        {
            return [];
        }

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
