using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PartnerEndpoints;

public class UpdatePartnerTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
        : PartnerTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PutAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange 
        var partner = _builder.PartnerBuilder
            .WithName("Partner to update")
            .WithAddress("partner's address to update")
            .WithEmail("partner's email to update")
            .WithCompanyName("partner's company name")
            .WithType(Domain.Enums.PartnerType.Customer)
            .WithPhoneNumbers(["+998914564561"])
            .Build();
        var partnerId = await CreatePartnerAsync(partner);
        var request = PartnerRequestFactory.GenerateValidUpdateRequest(partnerId);
        var url = GetUrl(partnerId);

        // Act
        var response = await _client.PutAsync<UpdatePartnerResponse>(url, request);

        // Assert
        await _responseValidator.Partner.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenPartnerDoesNotExist()
    {
        // Arrange
        var request = PartnerRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act 
        var response = await _client.PutAsync<ProblemDetails>(NotFoundUrl, request, HttpStatusCode.NotFound);

        // Assert 
        response.ShouldBeNotFound<Partner>(NonExistentEntityId);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var partnerId = await CreatePartnerAsync();
        var request = PartnerRequestFactory.GenerateInvalidUpdateRequest(partnerId);
        var url = GetUrl(partnerId);

        // Act
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Partner.Name), response.Errors.Keys);
        Assert.Contains(nameof(Partner.Email), response.Errors.Keys);
    }
}
