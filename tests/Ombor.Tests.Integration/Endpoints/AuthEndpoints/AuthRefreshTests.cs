using System.Net;
using Ombor.Domain.Exceptions;
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
        using var client = CreateRawClient();
        var login = await SendAsync(client, HttpMethod.Post, "auth/login", new { phoneNumber = phone, password = Password });

        var refreshed = await SendAsync(client, HttpMethod.Post, "auth/refresh-token", refreshTokenCookie: login.RefreshTokenCookie);

        Assert.Equal(HttpStatusCode.OK, refreshed.Status);
        Assert.False(string.IsNullOrEmpty((string?)refreshed.Body["accessToken"]));
        Assert.False(string.IsNullOrEmpty(refreshed.RefreshTokenCookie));
        Assert.NotEqual(login.RefreshTokenCookie, refreshed.RefreshTokenCookie);
    }

    [Fact]
    public async Task Login_AndRefresh_KeepTheRefreshTokenOutOfTheBody()
    {
        // backend-12: the long-lived token travels only in the httpOnly cookie, where page script cannot read it.
        var (_, phone) = await SeedUserAsync(Password);
        using var client = CreateRawClient();

        var login = await SendAsync(client, HttpMethod.Post, "auth/login", new { phoneNumber = phone, password = Password });
        var refreshed = await SendAsync(client, HttpMethod.Post, "auth/refresh-token", refreshTokenCookie: login.RefreshTokenCookie);

        Assert.NotNull(login.RefreshTokenCookie);
        Assert.Null(login.Body["refreshToken"]);
        Assert.Equal("ru", (string?)login.Body["language"]);
        Assert.Null(refreshed.Body["refreshToken"]);
    }

    [Fact]
    public async Task RefreshToken_SentInTheBody_IsNotAccepted()
    {
        var (_, phone) = await SeedUserAsync(Password);
        using var client = CreateRawClient();
        var login = await SendAsync(client, HttpMethod.Post, "auth/login", new { phoneNumber = phone, password = Password });

        var refresh = await SendAsync(client, HttpMethod.Post, "auth/refresh-token", new { refreshToken = login.RefreshTokenCookie });

        Assert.Equal(HttpStatusCode.Unauthorized, refresh.Status);
        Assert.Equal(ErrorCodes.SessionExpired, refresh.Code);
    }
}
