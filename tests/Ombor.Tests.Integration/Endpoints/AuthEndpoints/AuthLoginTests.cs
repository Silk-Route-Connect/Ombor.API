using System.Net;
using Ombor.Contracts.Responses.Auth;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

public sealed class AuthLoginTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    [Fact]
    public async Task Login_ReturnsTheUsersSavedLanguage()
    {
        // A18: login carries the saved language so a fresh device restores the user's interface locale.
        var (_, phone) = await SeedUserAsync("OldPassw0rd");

        var login = await _client.PostAsync<LoginResponse>(
            "auth/login", new { phoneNumber = phone, password = "OldPassw0rd" }, HttpStatusCode.OK);

        Assert.Equal("ru", login.Language); // SeedUserAsync leaves the default User.Language ("ru")
    }
}
