using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
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
            .WithName("partner to update")
            .WithAddress("partner's address to update")
            .WithEmail("partner's email to update")
            .WithCompanyName("partner's company name")
            .WithType(Domain.Enums.PartnerType.Customer)
            .WithPhoneNumbers(["+998914564561"])
            .Build();

        var partnerId = await CreatePartnerAsync(partner);
        var request = CreateValidRequest(partnerId);
        var url = GetUrl(partnerId);

        // Act
        var response = await _client.PutAsync<UpdatePartnerResponse>(url, request);

        // Assert
        await _responseValidator.partner.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenpartnerDoesNotExist()
    {
        // Arrange
        var request = CreateValidRequest(NonExistentEntityId);

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
        var request = CreateInvalidRequest(partnerId);
        var url = GetUrl(partnerId);

        // Act
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Partner.Name), response.Errors.Keys);
    }

    private static UpdatePartnerRequest CreateValidRequest(int id) =>
        new(Id: id,
            Name: "Updated partner name",
            Address: "Updated address",
            Email: "Updated email",
            CompanyName: "Updated company name",
            Balance: 1500.00m,
            Type: PartnerType.Supplier,
            PhoneNumbers: ["+998912322323"]);

    private static UpdatePartnerRequest CreateInvalidRequest(int id) =>
        new(Id: id,
            Name: string.Empty,
            Address: string.Empty,
            Email: string.Empty,
            CompanyName: string.Empty,
            Balance: 0m,
            Type: PartnerType.Supplier,
            PhoneNumbers: ["+asdsgasd"]);
}
