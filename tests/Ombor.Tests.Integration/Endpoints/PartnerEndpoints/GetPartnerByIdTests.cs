using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PartnerEndpoints;

public class GetPartnerByIdTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
        : PartnerTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk_WhenpartnerExists()
    {
        // Arrange 
        var partner = _builder.partnerBuilder
            .WithName("partner To Be Fetched")
            .Build();

        var partnerId = await CreatePartnerAsync(partner);
        var url = GetUrl(partnerId);

        // Act
        var response = await _client.GetAsync<PartnerDto>(url);

        // Assert
        await _responseValidator.partner.ValidateGetByIdAsync(partnerId, response);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenpartnerDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync<ProblemDetails>(NotFoundUrl, System.Net.HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Partner>(NonExistentEntityId);
    }
}
