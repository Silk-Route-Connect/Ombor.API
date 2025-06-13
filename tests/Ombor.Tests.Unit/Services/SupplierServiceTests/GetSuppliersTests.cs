
using Moq;
using Ombor.Contracts.Requests.Supplier;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.SupplierServiceTests;

public sealed class GetSuppliersTests : SupplierTestsBase
{
    private const string MatchingSearchTerm = "Test Match";

    public static TheoryData<GetSuppliersRequest> GetRequests => new()
    {
        {new GetSuppliersRequest(null)},
        {new GetSuppliersRequest(string.Empty)},
        {new GetSuppliersRequest(" ")},
        {new GetSuppliersRequest("     ")},
        {new GetSuppliersRequest(MatchingSearchTerm)},
    };

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetSuppliersRequest request = null!;

        // Act & Assert 
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNoSuppliers()
    {
        // Arrange
        var request = new GetSuppliersRequest(string.Empty);
        SetupSuppliers([]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Empty(response);

        _mockContext.Verify(mock => mock.Suppliers, Times.Once);

        VerifyNoOtherCalls();
    }

    [Theory, MemberData(nameof(GetRequests))]
    public async Task GetAsync_ShouldReturnMatchingSuppliers(GetSuppliersRequest request)
    {
        // Arrange 
        var matchingSuppliers = CreateMatchingSuppliers(request);
        Partner[] allSuppliers = [.. _defaultSuppliers, .. matchingSuppliers];
        var expectedSuppliers = request.IsEmpty()
            ? allSuppliers : matchingSuppliers;

        SetupSuppliers([.. _defaultSuppliers, .. matchingSuppliers]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(expectedSuppliers.Length, response.Length);
        Assert.All(response, actual =>
        {
            var expected = expectedSuppliers.SingleOrDefault(x => x.Id == actual.Id);

            SupplierAssertionHelper.AssertEquivalent(expected, actual);
        });

        _mockContext.Verify(mock => mock.Suppliers, Times.Once);

        VerifyNoOtherCalls();
    }

    private Partner[] CreateMatchingSuppliers(GetSuppliersRequest request)
    {
        if (request.IsEmpty())
        {
            return [];
        }

        var matchingName = _builder.SupplierBuilder
        .WithId(113)
        .WithName(request.SearchTerm)
        .WithAddress("qwerew")
        .WithEmail("aasdasd@gmail.com")
        .WithCompanyName("1qwerty")
        .WithIsActive(true)
        .WithPhoneNumbers(["+998914445566"])
        .BuildAndPopulate();
        var matchingEmail = _builder.SupplierBuilder
        .WithId(123)
        .WithName("Supplier Name")
        .WithAddress("Supplier Address1")
        .WithEmail(request.SearchTerm)
        .WithCompanyName("qwerty2")
        .WithIsActive(true)
        .WithPhoneNumbers(["+998912142566"])
        .BuildAndPopulate();

        return [matchingName, matchingEmail];
    }
}
