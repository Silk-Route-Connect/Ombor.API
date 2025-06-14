using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TemplateEndpoints;

public class DeleteTemplateTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : TemplateTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldDeleteTemplate_WhenTemplateExists()
    {
        // Arrange
        var templateToDelete = await CreateTemplateAsync();
        var url = GetUrl(templateToDelete.Id);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Template.ValidateDeleteAsync(templateToDelete.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenTemplateDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Template>(NonExistentEntityId);
    }
}
