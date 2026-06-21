using System.Net;
using Ombor.Tests.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.SettingsEndpoints;

public sealed class SettingsOrganizationTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : SettingsTestsBase(factory, output)
{
    [Fact]
    public async Task UpdateOrganization_RoundTripsProfile()
    {
        // Arrange / Act
        var name = $"Acme {Guid.NewGuid():N}";
        var updated = await UpdateOrganizationAsync(name, address: "Tashkent, Chilonzor", phone: "+998901234567", email: "info@acme.uz");

        // Assert — the response echoes the update, and a subsequent GET returns the same.
        Assert.Equal(name, updated.Name);
        Assert.Equal("Tashkent, Chilonzor", updated.Address);
        Assert.Equal("+998901234567", updated.Phone);
        Assert.Equal("info@acme.uz", updated.Email);

        var fetched = await GetOrganizationAsync();
        Assert.Equal(name, fetched.Name);
        Assert.Equal("Tashkent, Chilonzor", fetched.Address);
        Assert.Equal("+998901234567", fetched.Phone);
        Assert.Equal("info@acme.uz", fetched.Email);
    }

    [Fact]
    public async Task UpdateOrganization_WithLogo_SetsLogoUrl()
    {
        var updated = await UpdateOrganizationAsync($"Acme {Guid.NewGuid():N}", withLogo: true);

        Assert.False(string.IsNullOrWhiteSpace(updated.LogoUrl));
    }

    [Fact]
    public async Task UpdateOrganization_OmittingLogo_KeepsExistingLogo()
    {
        // Arrange — upload a logo first.
        var withLogo = await UpdateOrganizationAsync($"Acme {Guid.NewGuid():N}", withLogo: true);
        Assert.False(string.IsNullOrWhiteSpace(withLogo.LogoUrl));

        // Act — a later update without a logo file keeps the existing one.
        var withoutLogo = await UpdateOrganizationAsync($"Acme {Guid.NewGuid():N}", withLogo: false);

        // Assert
        Assert.Equal(withLogo.LogoUrl, withoutLogo.LogoUrl);
    }

    [Fact]
    public async Task UpdateOrganization_BlankName_Returns400()
    {
        await _client.PutAsync(
            OrganizationUrl,
            BuildOrganizationForm(name: "", address: null, phone: null, email: null, withLogo: false),
            HttpStatusCode.BadRequest);
    }
}
