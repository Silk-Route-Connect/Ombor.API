using System.Net;
using Ombor.Contracts.Responses.Auth;
using Ombor.Contracts.Responses.User;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

/// <summary>mvp «a second user joins the same organization»: invite → forgot password → reset → login.</summary>
public sealed class InvitedUserSignInTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    private const string Password = "FirstPassw0rd";

    [Fact]
    public async Task InvitedUser_CompletesFirstSignIn_ThroughForgotPassword()
    {
        var phone = NewPhone();
        var national = phone[4..];

        var invited = await _client.PostAsync<TenantUserDto>(
            "settings/users/invite",
            new { method = "Phone", value = $"{national[..2]} {national[2..5]} {national[5..7]} {national[7..]}", firstName = "Dilshod", lastName = "Karimov" },
            HttpStatusCode.Created);

        Assert.Equal(phone, invited.Contact); // stored canonical
        Assert.Equal("Dilshod Karimov", invited.Name);

        // Before the reset the account is unconfirmed: the same 401 as any wrong credential.
        var beforeReset = await PostRawAsync("auth/login", new { phoneNumber = phone, password = Password });
        Assert.Equal(HttpStatusCode.Unauthorized, beforeReset.Status);
        Assert.Equal(ErrorCodes.InvalidCredentials, beforeReset.Code);

        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.PasswordReset);
        var reset = await _client.PostAsync<ResetPasswordResponse>(
            "auth/reset-password",
            new { phoneNumber = phone, code, newPassword = Password, confirmPassword = Password },
            HttpStatusCode.OK);
        Assert.True(reset.Success);

        var login = await _client.PostAsync<LoginResponse>(
            "auth/login", new { phoneNumber = phone, password = Password }, HttpStatusCode.OK);
        Assert.False(string.IsNullOrEmpty(login.AccessToken));
    }

    [Fact]
    public async Task Invite_WithoutNames_ShowsThePhone()
    {
        var phone = NewPhone();

        var invited = await _client.PostAsync<TenantUserDto>(
            "settings/users/invite", new { method = "Phone", value = phone }, HttpStatusCode.Created);

        Assert.Equal(phone, invited.Name);
    }

    [Fact]
    public async Task Invite_RegisteredPhone_Is400PhoneTaken()
    {
        var (_, phone) = await SeedUserAsync(Password);

        using var client = CreateTestAuthClient();
        var response = await SendAsync(client, HttpMethod.Post, "settings/users/invite", new { method = "Phone", value = phone });

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal(ErrorCodes.PhoneTaken, response.Code);
        Assert.NotNull(response.Body["errors"]!["Value"]);
    }

    [Fact]
    public async Task Invite_MalformedPhone_Is400()
    {
        using var client = CreateTestAuthClient();
        var response = await SendAsync(client, HttpMethod.Post, "settings/users/invite", new { method = "Phone", value = "12345" });

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal(ErrorCodes.ValidationFailed, response.Code);
    }

    private HttpClient CreateTestAuthClient()
    {
        var client = CreateRawClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Test");

        return client;
    }
}
