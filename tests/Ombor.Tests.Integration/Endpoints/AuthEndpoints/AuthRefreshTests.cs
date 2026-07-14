using System.Net;
using Ombor.Contracts.Responses.Auth;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

public sealed class AuthRefreshTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    private const string Password = "OldPassw0rd";

    [Fact]
    public async Task RefreshToken_IssuesNewTokens_ForAValidRefreshToken()
    {
        // Regression guard for the refresh path: the user re-fetch bypasses the org query filter, so a valid
        // refresh token still resolves its user and yields fresh tokens.
        var (_, phone) = await SeedUserAsync(Password);

        var login = await _client.PostAsync<LoginResponse>(
            "auth/login", new { phoneNumber = phone, password = Password }, HttpStatusCode.OK);

        var refreshed = await _client.PostAsync<RefreshTokenResponse>(
            "auth/refresh-token", new { refreshToken = login.RefreshToken }, HttpStatusCode.OK);

        Assert.False(string.IsNullOrEmpty(refreshed.AccessToken));
        Assert.False(string.IsNullOrEmpty(refreshed.RefreshToken));
    }
}
