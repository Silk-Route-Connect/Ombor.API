using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Partner;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PartnerEndpoints;

public class CreatePartnerTests(TestingWebApplicationFactory factory, ITestOutputHelper testOutput)
        : PartnerTestsBase(factory, testOutput)
{
    [Fact]
    public async Task PostAsync_ShouldReturnCreated_WhenRequestIsValid()
    {
        // Arrange
        var request = PartnerRequestFactory.GenerateValidCreateRequest();

        // Act
        var response = await _client.PostAsync<CreatePartnerResponse>(Routes.Partner, request);

        // Assert
        await _responseValidator.Partner.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = PartnerRequestFactory.GenerateInvalidCreateRequest();

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(Routes.Partner, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Partner.Name), response.Errors.Keys);
        Assert.Contains(nameof(Partner.Email), response.Errors.Keys);
    }
}
