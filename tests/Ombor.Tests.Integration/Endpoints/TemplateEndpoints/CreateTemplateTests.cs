using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TemplateEndpoints;

public sealed class CreateTemplateTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : TemplateTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostAsync_ShouldReturnCreatedTemplate_WhenRequestIsValid()
    {
        // Arrange
        var partner = await CreatePartnerAsync("Partner for valid Template Create");
        var request = TemplateRequestFactory.GenerateValidCreateRequest(partnerId: partner.Id);
        var url = GetUrl();

        // Act
        var response = await _client.PostAsync<CreateTemplateResponse>(url, request);

        // Assert
        await _responseValidator.Template.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = TemplateRequestFactory.GenerateInvalidCreateRequest(partnerId: NonExistentEntityId);
        var url = GetUrl();

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Template.Name), response.Errors.Keys);
    }
}
