using System.Net;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

public sealed class OtpProtectionTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    private const string OldPassword = "OldPassw0rd";
    private const string NewPassword = "NewPassw0rd";

    [Fact]
    public async Task ResetCode_IsInvalidatedAfterFiveWrongGuesses_SoEvenTheRightCodeFailsAfterwards()
    {
        var (_, phone) = await SeedUserAsync(OldPassword);
        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.PasswordReset);
        var wrong = AuthRegisterTests.WrongCodeFor(code);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var guess = await VerifyAsync(phone, wrong);
            Assert.False(guess.Success);
            Assert.Equal(ErrorCodes.CodeInvalid, guess.Code);
        }

        var fifth = await VerifyAsync(phone, wrong);
        Assert.False(fifth.Success);
        Assert.Equal(ErrorCodes.TooManyAttempts, fifth.Code);

        var reset = await _client.PostAsync<ResetPasswordResponse>(
            "auth/reset-password",
            new { phoneNumber = phone, code, newPassword = NewPassword, confirmPassword = NewPassword },
            HttpStatusCode.OK);

        Assert.False(reset.Success);
        Assert.Equal(ErrorCodes.TooManyAttempts, reset.Code);

        // The password did not change.
        await _client.PostAsync<LoginResponse>("auth/login", new { phoneNumber = phone, password = OldPassword }, HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_SecondRequestInsideTheCooldown_Is429_ForKnownAndUnknownNumbersAlike()
    {
        var (_, registered) = await SeedUserAsync(OldPassword);

        foreach (var phone in new[] { registered, NewPhone() })
        {
            var first = await PostRawAsync("auth/forgot-password", new { phoneNumber = phone });
            var second = await PostRawAsync("auth/forgot-password", new { phoneNumber = phone });

            Assert.Equal(HttpStatusCode.OK, first.Status);
            Assert.Equal(6, (int)first.Body["codeLength"]!);
            Assert.Equal(60, (int)first.Body["resendAfterSeconds"]!);

            Assert.Equal(HttpStatusCode.TooManyRequests, second.Status);
            Assert.Equal(ErrorCodes.RateLimited, second.Code);
            Assert.InRange((int)second.Body["params"]!["retryAfterSeconds"]!, 1, 60);
            Assert.NotNull(second.RetryAfter);
        }
    }

    [Fact]
    public async Task VerifyResetCode_WithoutAnIssuedCode_IsCodeExpired()
    {
        var result = await VerifyAsync(NewPhone(), "123456");

        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.CodeExpired, result.Code);
    }

    private Task<VerifyResetCodeResponse> VerifyAsync(string phone, string code) =>
        _client.PostAsync<VerifyResetCodeResponse>(
            "auth/verify-reset-code", new { phoneNumber = phone, code }, HttpStatusCode.OK);
}
