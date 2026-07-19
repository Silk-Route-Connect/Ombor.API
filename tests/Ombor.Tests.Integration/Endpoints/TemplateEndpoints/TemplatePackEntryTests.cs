using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Responses.Template;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.TemplateEndpoints;

public sealed class TemplatePackEntryTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : TemplateTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PostThenGet_ShouldComputeBaseQuantityFromPackageEntry_AndSnapshotSize()
    {
        // Arrange — a product packaged 6-to-a-pack; an item entered as 3 packs = 18 base units. R21: server
        // computes the base quantity from the product's size and snapshots it; the client sends no size.
        var partner = await CreatePartnerAsync($"Partner pack {Guid.NewGuid()}");
        var productId = await CreateProductAsync(packageSize: 6);
        // The client's base Quantity (999) is deliberately wrong to prove the server recomputes 3 × 6.
        var request = new CreateTemplateRequest(
            partner.Id,
            $"Template {Guid.NewGuid():N}",
            TemplateType.Sale,
            [new CreateTemplateItem(ProductId: productId, Quantity: 999, UnitPrice: 1_000m, Discount: 0m, DiscountType: DiscountType.Fixed, PackageQuantity: 3)]);

        // Act
        var created = await _client.PostAsync<CreateTemplateResponse>(GetUrl(), request);
        var fetched = await _client.GetAsync<TemplateDto>(GetUrl(created.Id));

        // Assert — create response and a fresh read both carry the computed base quantity + the size snapshot.
        var createdItem = Assert.Single(created.Items);
        Assert.Equal(18m, createdItem.Quantity);
        Assert.Equal(6, createdItem.PackageSize);

        var fetchedItem = Assert.Single(fetched.Items);
        Assert.Equal(18m, fetchedItem.Quantity);
        Assert.Equal(6, fetchedItem.PackageSize);
    }

    [Fact]
    public async Task Put_ShouldComputeBaseQuantityFromPackageEntry_WhenUpdatingAnExistingItem()
    {
        // Arrange — a base-unit item, then updated in place to a pack entry (exercises the update-existing branch).
        var partner = await CreatePartnerAsync($"Partner pack upd {Guid.NewGuid()}");
        var productId = await CreateProductAsync(packageSize: 6);
        var created = await _client.PostAsync<CreateTemplateResponse>(GetUrl(), new CreateTemplateRequest(
            partner.Id,
            $"Template {Guid.NewGuid():N}",
            TemplateType.Sale,
            [new CreateTemplateItem(ProductId: productId, Quantity: 5, UnitPrice: 1_000m, Discount: 0m)]));
        var itemId = Assert.Single(created.Items).Id;

        var updateRequest = new UpdateTemplateRequest(
            created.Id,
            partner.Id,
            created.Name,
            TemplateType.Sale,
            [new UpdateTemplateItem(Id: itemId, ProductId: productId, Quantity: 999, UnitPrice: 1_000m, Discount: 0m, DiscountType: DiscountType.Fixed, PackageQuantity: 2)]);

        // Act
        await _client.PutAsync<UpdateTemplateResponse>(GetUrl(created.Id), updateRequest);
        var fetched = await _client.GetAsync<TemplateDto>(GetUrl(created.Id));

        // Assert — 2 packs × 6 = 12 base units, size snapshotted.
        var item = Assert.Single(fetched.Items);
        Assert.Equal(12m, item.Quantity);
        Assert.Equal(6, item.PackageSize);
    }
}
