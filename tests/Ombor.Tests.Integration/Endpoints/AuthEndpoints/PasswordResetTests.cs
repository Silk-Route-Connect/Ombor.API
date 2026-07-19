using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

public sealed class PasswordResetTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    private const string OldPassword = "OldPassw0rd";
    private const string NewPassword = "NewPassw0rd";

    [Fact]
    public async Task FullFlow_ResetsPassword_AndLetsUserLogInWithTheNewPassword()
    {
        // Arrange — an existing user with a known old password.
        var (_, phone) = await SeedUserAsync(OldPassword);

        // Act — request a reset, then reset with the code the server actually issued.
        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.PasswordReset);
        var reset = await _client.PostAsync<ResetPasswordResponse>(
            "auth/reset-password",
            new { phoneNumber = phone, code, newPassword = NewPassword, confirmPassword = NewPassword },
            HttpStatusCode.OK);

        // Assert — reset succeeded and the user can now log in with the NEW password (proving the hash changed).
        Assert.True(reset.Success);
        var login = await _client.PostAsync<LoginResponse>(
            "auth/login", new { phoneNumber = phone, password = NewPassword }, HttpStatusCode.OK);
        Assert.False(string.IsNullOrEmpty(login.AccessToken));
    }

    [Fact]
    public async Task ForgotPassword_UnknownPhone_ReturnsGenericOk_WithoutEnumeration()
    {
        // A number with no account returns the same generic 200 (no throw, no "not found") — no enumeration.
        var response = await _client.PostAsync<ForgotPasswordResponse>(
            "auth/forgot-password", new { phoneNumber = NewPhone() }, HttpStatusCode.OK);

        Assert.Equal(5, response.ExpiresInMinutes);
        Assert.False(string.IsNullOrEmpty(response.Message));
    }

    [Fact]
    public async Task VerifyResetCode_ReturnsSuccessForCorrectCode_AndFalseForWrong()
    {
        var (_, phone) = await SeedUserAsync(OldPassword);
        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.PasswordReset);

        var ok = await _client.PostAsync<VerifyResetCodeResponse>(
            "auth/verify-reset-code", new { phoneNumber = phone, code }, HttpStatusCode.OK);
        var wrong = await _client.PostAsync<VerifyResetCodeResponse>(
            "auth/verify-reset-code", new { phoneNumber = phone, code = WrongCodeFor(code) }, HttpStatusCode.OK);

        Assert.True(ok.Success);
        Assert.False(wrong.Success);
    }

    [Theory]
    [InlineData("short", "short")]          // below the 8-char minimum
    [InlineData("ValidPass1", "Different1")] // confirmation mismatch
    public async Task ResetPassword_RejectsWeakOrMismatchedPassword_With400(string newPassword, string confirmPassword)
    {
        var (_, phone) = await SeedUserAsync(OldPassword);
        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.PasswordReset);

        // Password validation fires before the code check, so the (valid) code never gets that far — still a 400.
        var problem = await _client.PostAsync<ValidationProblemDetails>(
            "auth/reset-password",
            new { phoneNumber = phone, code, newPassword, confirmPassword },
            HttpStatusCode.BadRequest);

        Assert.NotNull(problem);
    }

    [Fact]
    public async Task ResetPassword_WithWrongCode_ReturnsSuccessFalse()
    {
        var (_, phone) = await SeedUserAsync(OldPassword);
        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.PasswordReset);

        var reset = await _client.PostAsync<ResetPasswordResponse>(
            "auth/reset-password",
            new { phoneNumber = phone, code = WrongCodeFor(code), newPassword = NewPassword, confirmPassword = NewPassword },
            HttpStatusCode.OK);

        Assert.False(reset.Success);
    }

    [Fact]
    public async Task ResetPassword_RevokesTheUsersExistingSessions()
    {
        // Arrange — an existing user with an active refresh token (a live session).
        var (userId, phone) = await SeedUserAsync(OldPassword);
        var token = new RefreshToken
        {
            Token = $"tok-{Guid.NewGuid():N}",
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            UserId = userId,
            User = null!,
        };
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync();

        // Act
        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.PasswordReset);
        await _client.PostAsync<ResetPasswordResponse>(
            "auth/reset-password",
            new { phoneNumber = phone, code, newPassword = NewPassword, confirmPassword = NewPassword },
            HttpStatusCode.OK);

        // Assert — the pre-existing session was revoked.
        var refreshed = await _context.RefreshTokens.AsNoTracking().FirstAsync(t => t.Id == token.Id);
        Assert.True(refreshed.IsRevoked);
    }

    // A 4-digit code guaranteed to differ from the issued one (the issued code is random, so a fixed literal
    // could, rarely, collide with it).
    private static string WrongCodeFor(string code) => code == "0000" ? "1111" : "0000";
}
