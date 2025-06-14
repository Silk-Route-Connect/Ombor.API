using System.Net;
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
    public async Task GetByIdAsync_ShouldReturnOk_WhenPartnerExists()
    {
        // Arrange 
        var partner = _builder.PartnerBuilder
            .WithName("Partner To Be Fetch By ID")
            .Build();
        var partnerId = await CreatePartnerAsync(partner);
        var url = GetUrl(partnerId);

        // Act
        var response = await _client.GetAsync<PartnerDto>(url);

        // Assert
        await _responseValidator.Partner.ValidateGetByIdAsync(partnerId, response);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenPartnerDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Partner>(NonExistentEntityId);
    }
}
