using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Template;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TemplateEndpoints;

public class UpdateTemplateTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : TemplateTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PutAsync_ShouldReturnUpdatedTemplate_WhenRequestIsValid()
    {
        // Arrange
        var templateToUpdate = await CreateTemplateAsync();
        var request = TemplateRequestFactory.GenerateValidUpdateRequest(templateToUpdate.Id, templateToUpdate.Items);
        var url = GetUrl(templateToUpdate.Id);

        // Act
        var response = await _client.PutAsync<UpdateTemplateResponse>(url, request);

        // Assert
        await _responseValidator.Template.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenTemplateDoesNotExist()
    {
        // Arrange
        var url = NotFoundUrl;
        var request = TemplateRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId, []);

        // Act
        var response = await _client.PutAsync<ProblemDetails>(url, request, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Template>(NonExistentEntityId);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var templateToUpdate = await CreateTemplateAsync();
        var request = TemplateRequestFactory.GenerateInvalidUpdateRequest(templateToUpdate.Id);
        var url = GetUrl(templateToUpdate.Id);

        // Act
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Template.Name), response.Errors.Keys);
    }
}
