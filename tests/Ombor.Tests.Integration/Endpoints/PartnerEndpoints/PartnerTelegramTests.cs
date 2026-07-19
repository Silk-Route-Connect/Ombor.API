using Ombor.Contracts.Responses.Partner;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PartnerEndpoints;

public sealed class PartnerTelegramTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : PartnerTestsBase(factory, output)
{
    [Fact]
    public async Task CreateThenUpdate_ShouldRoundTripTelegramHandle()
    {
        // Arrange — create a partner with a Telegram handle (F12).
        var createRequest = PartnerRequestFactory.GenerateValidCreateRequest() with { Telegram = "@ombor_partner" };

        // Act — create
        var created = await _client.PostAsync<CreatePartnerResponse>(GetUrl(), createRequest);

        // Assert — the create response and a fresh read both carry the handle.
        Assert.Equal("@ombor_partner", created.Telegram);
        var fetched = await _client.GetAsync<PartnerDto>(GetUrl(created.Id));
        Assert.Equal("@ombor_partner", fetched.Telegram);

        // Act — update the handle.
        var updateRequest = PartnerRequestFactory.GenerateValidUpdateRequest(created.Id) with { Telegram = "@ombor_updated" };
        var updated = await _client.PutAsync<UpdatePartnerResponse>(GetUrl(created.Id), updateRequest);

        // Assert — the update response and a fresh read reflect the new handle.
        Assert.Equal("@ombor_updated", updated.Telegram);
        var refetched = await _client.GetAsync<PartnerDto>(GetUrl(created.Id));
        Assert.Equal("@ombor_updated", refetched.Telegram);
    }

    [Fact]
    public async Task Create_ShouldLeaveTelegramNull_WhenNotProvided()
    {
        // Arrange — the factory sends no Telegram.
        var createRequest = PartnerRequestFactory.GenerateValidCreateRequest();

        // Act
        var created = await _client.PostAsync<CreatePartnerResponse>(GetUrl(), createRequest);
        var fetched = await _client.GetAsync<PartnerDto>(GetUrl(created.Id));

        // Assert
        Assert.Null(created.Telegram);
        Assert.Null(fetched.Telegram);
    }
}
