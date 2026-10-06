using System.Net;
using Microsoft.EntityFrameworkCore;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

/// <summary>
/// Runs on a host that authenticates with real JWT bearer tokens (no always-signed-in test scheme), so the access
/// token itself is under test and anonymous requests carry no organization.
/// </summary>
public sealed class AuthSessionTests(JwtTestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output), IClassFixture<JwtTestingWebApplicationFactory>
{
    private const string Password = "Passw0rd!";
    private const string NewPassword = "N3wPassw0rd!";

    [Fact]
    public async Task DeactivatedUser_LosesTheAccessToken_AndTheRefreshToken()
    {
        using var client = CreateRawClient();
        var (_, adminPhone) = await SeedUserAsync(Password);
        var (staffId, staffPhone) = await SeedUserAsync(Password);
        var admin = await LoginAsync(client, adminPhone, Password);
        var staff = await LoginAsync(client, staffPhone, Password);

        var before = await SendAsync(client, HttpMethod.Get, "settings/users", bearerToken: staff.AccessToken);
        Assert.Equal(HttpStatusCode.OK, before.Status);

        var deactivate = await SendAsync(
            client, HttpMethod.Post, $"settings/users/{staffId}/deactivate", new { }, bearerToken: admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, deactivate.Status);

        // The still-unexpired access token stops working on the next request (rule 41) …
        var after = await SendAsync(client, HttpMethod.Get, "settings/users", bearerToken: staff.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, after.Status);
        Assert.Equal(ErrorCodes.AccountDeactivated, after.Code);

        // … and the refresh token cannot mint a new one.
        var refresh = await SendAsync(client, HttpMethod.Post, "auth/refresh-token", refreshTokenCookie: staff.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.Status);
        Assert.Equal(ErrorCodes.AccountDeactivated, refresh.Code);

        // The admin is unaffected.
        var adminCall = await SendAsync(client, HttpMethod.Get, "settings/users", bearerToken: admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, adminCall.Status);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Is400OnTheCurrentPasswordField()
    {
        using var client = CreateRawClient();
        var (_, phone) = await SeedUserAsync(Password);
        var session = await LoginAsync(client, phone, Password);

        var response = await SendAsync(
            client,
            HttpMethod.Put,
            "settings/password",
            new { currentPassword = "Wrong-Passw0rd", newPassword = NewPassword, confirmPassword = NewPassword },
            bearerToken: session.AccessToken,
            refreshTokenCookie: session.RefreshToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal(ErrorCodes.CurrentPasswordInvalid, response.Code);
        Assert.NotNull(response.Body["errors"]!["CurrentPassword"]);
    }

    [Fact]
    public async Task ChangePassword_KeepsThisSession_AndSignsOutEveryOtherOne()
    {
        using var client = CreateRawClient();
        var (_, phone) = await SeedUserAsync(Password);
        var thisDevice = await LoginAsync(client, phone, Password);
        var otherDevice = await LoginAsync(client, phone, Password);

        var change = await SendAsync(
            client,
            HttpMethod.Put,
            "settings/password",
            new { currentPassword = Password, newPassword = NewPassword, confirmPassword = NewPassword },
            bearerToken: thisDevice.AccessToken,
            refreshTokenCookie: thisDevice.RefreshToken);
        Assert.Equal(HttpStatusCode.NoContent, change.Status);

        var otherRefresh = await SendAsync(client, HttpMethod.Post, "auth/refresh-token", refreshTokenCookie: otherDevice.RefreshToken);
        Assert.Equal(HttpStatusCode.Unauthorized, otherRefresh.Status);
        Assert.Equal(ErrorCodes.SessionExpired, otherRefresh.Code);

        var thisRefresh = await SendAsync(client, HttpMethod.Post, "auth/refresh-token", refreshTokenCookie: thisDevice.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, thisRefresh.Status);

        var oldPassword = await SendAsync(client, HttpMethod.Post, "auth/login", new { phoneNumber = phone, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.Status);
        await LoginAsync(client, phone, NewPassword);
    }

    [Fact]
    public async Task Register_ThenVerify_CreatesTheAccount_UnderTheCanonicalPhone()
    {
        using var client = CreateRawClient();
        var phone = NewPhone();
        var national = phone[4..];

        var register = await SendAsync(
            client,
            HttpMethod.Post,
            "auth/register",
            new
            {
                firstName = "Ali",
                lastName = "Valiyev",
                phoneNumber = $"({national[..2]}) {national[2..5]}-{national[5..7]}-{national[7..]}",
                password = Password,
                confirmPassword = Password,
                organizationName = "Ali Shop",
                email = "   ",
            },
            language: "ru");
        Assert.Equal(HttpStatusCode.OK, register.Status);

        var code = await GetIssuedOtpAsync(phone, OtpPurpose.Registration);
        var verify = await SendAsync(
            client, HttpMethod.Post, "auth/verification", new { phoneNumber = phone, code }, language: "ru");
        Assert.Equal(HttpStatusCode.OK, verify.Status);
        Assert.False(string.IsNullOrEmpty((string?)verify.Body["accessToken"]));

        var user = await _context.Users.IgnoreQueryFilters().AsNoTracking().SingleAsync(u => u.PhoneNumber == phone);
        Assert.True(user.IsPhoneNumberConfirmed);
        Assert.Null(user.Email); // a blank email is stored as null, so it cannot collide on the unique index

        await LoginAsync(client, phone, Password);
    }

    private static async Task<Session> LoginAsync(HttpClient client, string phone, string password)
    {
        var login = await SendAsync(client, HttpMethod.Post, "auth/login", new { phoneNumber = phone, password });
        Assert.Equal(HttpStatusCode.OK, login.Status);

        return new Session((string)login.Body["accessToken"]!, login.RefreshTokenCookie!);
    }

    private sealed record Session(string AccessToken, string RefreshToken);
}
