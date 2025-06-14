
using Moq;
using Ombor.Contracts.Requests.Partner;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.PartnerServiceTests;

public sealed class GetPartnersTests : PartnerTestsBase
{
    private const string MatchingSearchTerm = "Test Match";

    public static TheoryData<GetPartnersRequest> GetRequests => new()
    {
        {new GetPartnersRequest(null)},
        {new GetPartnersRequest(string.Empty)},
        {new GetPartnersRequest(" ")},
        {new GetPartnersRequest("     ")},
        {new GetPartnersRequest(MatchingSearchTerm)},
    };

    [Fact]
    public async Task GetAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        GetPartnersRequest request = null!;

        // Act & Assert 
        await Assert.ThrowsAsync<ArgumentNullException>(
            nameof(request),
            () => _service.GetAsync(request));

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnEmpty_WhenNopartners()
    {
        // Arrange
        var request = new GetPartnersRequest(string.Empty);
        Setuppartners([]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Empty(response);

        _mockContext.Verify(mock => mock.Partners, Times.Once);

        VerifyNoOtherCalls();
    }

    [Theory, MemberData(nameof(GetRequests))]
    public async Task GetAsync_ShouldReturnMatchingpartners(GetPartnersRequest request)
    {
        // Arrange 
        var matchingpartners = CreateMatchingpartners(request);
        Partner[] allpartners = [.. _defaultpartners, .. matchingpartners];
        var expectedpartners = request.IsEmpty()
            ? allpartners : matchingpartners;

        Setuppartners([.. _defaultpartners, .. matchingpartners]);

        // Act
        var response = await _service.GetAsync(request);

        // Assert
        Assert.Equal(expectedpartners.Length, response.Length);
        Assert.All(response, actual =>
        {
            var expected = expectedpartners.SingleOrDefault(x => x.Id == actual.Id);

            PartnerAssertionHelper.AssertEquivalent(expected, actual);
        });

        _mockContext.Verify(mock => mock.Partners, Times.Once);

        VerifyNoOtherCalls();
    }

    private Partner[] CreateMatchingpartners(GetPartnersRequest request)
    {
        if (request.IsEmpty())
        {
            return [];
        }

        var matchingName = _builder.PartnerBuilder
            .WithId(113)
            .WithName(request.SearchTerm)
            .WithAddress("qwerew")
            .WithEmail("aasdasd@gmail.com")
            .WithCompanyName("1qwerty")
            .WithType(Domain.Enums.PartnerType.Customer)
            .WithPhoneNumbers(["+998914445566"])
            .BuildAndPopulate();

        var matchingEmail = _builder.PartnerBuilder
            .WithId(123)
            .WithName("partner Name")
            .WithAddress("partner Address1")
            .WithEmail(request.SearchTerm)
            .WithCompanyName("qwerty2")
            .WithType(Domain.Enums.PartnerType.Supplier)
            .WithPhoneNumbers(["+998912142566"])
            .BuildAndPopulate();

        return [matchingName, matchingEmail];
    }
}
