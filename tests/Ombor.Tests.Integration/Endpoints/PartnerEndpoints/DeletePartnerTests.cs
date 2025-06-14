using System.Net;
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
    public async Task DeleteAsync_ShouldReturnNoContent_WhenPartnerExists()
    {
        // Arrange 
        var partnerToDelete = _builder.PartnerBuilder
            .WithName("Partner To Delete")
            .Build();
        var partnerId = await CreatePartnerAsync(partnerToDelete);
        var url = GetUrl(partnerId);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Partner.ValidateDeleteAsync(partnerId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenPartnerDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert 
        response.ShouldBeNotFound<Partner>(NonExistentEntityId);
    }
}
