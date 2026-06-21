using System.Net;
using Ombor.Tests.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.SettingsEndpoints;

public sealed class SettingsUserTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : SettingsTestsBase(factory, output)
{
    [Fact]
    public async Task GetUsers_IncludesCurrentUser_WithSelfFlag()
    {
        var users = await GetUsersAsync();

        var self = Assert.Single(users, u => u.Id == CurrentUserId);
        Assert.True(self.Self);
        Assert.True(self.Active);
        Assert.Equal("phone", self.ContactType);
        Assert.False(self.Online);
        Assert.Null(self.LastActiveAt);
    }

    [Fact]
    public async Task InviteUser_ByPhone_CreatesActiveUser_AndAppearsInList()
    {
        var phone = UniquePhone();

        var invited = await InviteUserAsync(phone);

        Assert.True(invited.Active);
        Assert.False(invited.Self);
        Assert.Equal(phone, invited.Contact);
        Assert.Equal("phone", invited.ContactType);
        Assert.Null(invited.LastActiveAt);

        var users = await GetUsersAsync();
        Assert.Contains(users, u => u.Id == invited.Id && u.Contact == phone);
    }

    [Fact]
    public Task InviteUser_ByEmail_Returns400() =>
        _client.PostAsync<object>($"{UsersUrl}/invite", new { method = "Email", value = "user@acme.uz" }, HttpStatusCode.BadRequest);

    [Fact]
    public async Task InviteUser_DuplicatePhone_Returns400()
    {
        var phone = UniquePhone();
        await InviteUserAsync(phone);

        await _client.PostAsync<object>($"{UsersUrl}/invite", new { method = "Phone", value = phone }, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeactivateUser_SetsInactive_AndKeepsListed()
    {
        var invited = await InviteUserAsync(UniquePhone());

        var deactivated = await DeactivateUserAsync(invited.Id);
        Assert.False(deactivated.Active);
        Assert.NotNull(deactivated.LastActiveAt); // surfaces as the last-active date

        // Deactivated users are kept (rule 41), so they remain in the list.
        var listed = Assert.Single(await GetUsersAsync(), u => u.Id == invited.Id);
        Assert.False(listed.Active);
    }

    [Fact]
    public Task DeactivateSelf_Returns400() =>
        _client.PostAsync($"{UsersUrl}/{CurrentUserId}/deactivate", HttpStatusCode.BadRequest);

    [Fact]
    public Task DeactivateUser_NotFound_Returns404() =>
        _client.PostAsync($"{UsersUrl}/{NonExistentEntityId}/deactivate", HttpStatusCode.NotFound);

    [Fact]
    public async Task ReactivateUser_RestoresActive()
    {
        var invited = await InviteUserAsync(UniquePhone());
        await DeactivateUserAsync(invited.Id);

        var reactivated = await ReactivateUserAsync(invited.Id);
        Assert.True(reactivated.Active);
        Assert.Null(reactivated.LastActiveAt);
    }

    [Fact]
    public async Task SetLanguage_ValidSucceeds_InvalidReturns400()
    {
        await SetLanguageAsync("uz-Latn");
        await SetLanguageAsync("klingon", HttpStatusCode.BadRequest);
    }
}
