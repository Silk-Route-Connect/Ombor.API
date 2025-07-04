using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Template;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TemplateEndpoints;

public class GetTemplateByIdTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : TemplateTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenTemplateDoesNotExist()
    {
        // Arrange
        var url = NotFoundUrl;

        // Act
        var response = await _client.GetAsync<ProblemDetails>(url, System.Net.HttpStatusCode.NotFound);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(NotFoundTitle, response.Title);
    }

    [Fact]
    public async Task GetById_ShouldReturnTemplate_WhenTemplateExists()
    {
        // Arrange
        var partner = await CreatePartnerAsync("Partner for fetching Template by ID");
        var template = await CreateTemplateAsync(partner);
        var url = GetUrl(template.Id);

        // Act
        var response = await _client.GetAsync<TemplateDto>(url);

        // Assert
        await _responseValidator.Template.ValidateGetByIdAsnc(template.Id, response);
    }
}
