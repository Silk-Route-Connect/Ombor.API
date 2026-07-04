using System.Net;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TemplateEndpoints;

public sealed class TemplateUseAndDiscountTypeTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : TemplateTestsBase(factory, outputHelper)
{
    [Theory]
    [InlineData(DiscountType.Percentage)]
    [InlineData(DiscountType.Fixed)]
    public async Task PostThenGet_ShouldRoundTripItemDiscountType(DiscountType discountType)
    {
        // Arrange — the discount type must round-trip. It used to be dropped by the DTO/mapping and silently
        // defaulted to Fixed, so the frontend read fixed amounts as percentages (the "absurd totals" bug).
        var partner = await CreatePartnerAsync($"Partner discountType {Guid.NewGuid()}");
        var productId = await CreateProductAsync();
        var request = new CreateTemplateRequest(
            partner.Id,
            $"Template {Guid.NewGuid():N}",
            TemplateType.Sale,
            [new CreateTemplateItem(ProductId: productId, Quantity: 2, UnitPrice: 1_000m, Discount: 10m, DiscountType: discountType)]);

        // Act
        var created = await _client.PostAsync<CreateTemplateResponse>(GetUrl(), request);
        var fetched = await _client.GetAsync<TemplateDto>(GetUrl(created.Id));

        // Assert — the create response and a fresh read both preserve the requested discount type.
        Assert.Equal(discountType.ToString(), Assert.Single(created.Items).DiscountType);
        Assert.Equal(discountType.ToString(), Assert.Single(fetched.Items).DiscountType);
    }

    [Fact]
    public async Task Use_ShouldStampLastUsedAt()
    {
        // Arrange — a freshly created template has never been used.
        var partner = await CreatePartnerAsync($"Partner use {Guid.NewGuid()}");
        var template = await CreateTemplateAsync(partner);
        Assert.Null((await _client.GetAsync<TemplateDto>(GetUrl(template.Id))).LastUsedAt);

        // Act — loading it into a transaction stamps LastUsedAt.
        var used = await _client.PostAsync<TemplateDto>($"{GetUrl(template.Id)}/use", new { }, HttpStatusCode.OK);

        // Assert — stamped on the response and persisted for the next read.
        Assert.NotNull(used.LastUsedAt);
        Assert.NotNull((await _client.GetAsync<TemplateDto>(GetUrl(template.Id))).LastUsedAt);
    }
}
