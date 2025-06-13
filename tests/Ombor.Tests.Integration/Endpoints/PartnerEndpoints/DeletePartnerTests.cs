using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PartnerEndpoints;

public class DeletePartnerTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PartnerTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenpartnerExists()
    {
        // Arrange 
        var partnerToDelete = _builder.partnerBuilder
            .WithName("partner To Delete")
            .Build();

        var partnerId = await CreatePartnerAsync(partnerToDelete);
        var url = GetUrl(partnerId);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.partner.ValidateDeleteAsync(partnerId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenpartnerDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, System.Net.HttpStatusCode.NotFound);

        // Assert 
        response.ShouldBeNotFound<Partner>(NonExistentEntityId);
    }
}
