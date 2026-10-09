using System.Net;
using Microsoft.EntityFrameworkCore;
using Ombor.Application.Localization;
using Ombor.Application.Models;
using Ombor.Contracts.Responses.Auth;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

/// <summary>
/// The registration code goes out in the interface language of the sign-up (the <c>X-Ombor-Language</c> header), the
/// reset code in the account's saved language.
/// </summary>
public sealed class SmsLanguageTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    [Theory]
    [InlineData("ru")]
    [InlineData("uz-Latn")]
    [InlineData("uz-Cyrl")]
    public async Task Register_ShouldSendTheCode_InTheInterfaceLanguage(string language)
    {
        var phone = NewPhone();

        var response = await PostRawAsync("auth/register", AuthRegisterTests.Registration(phone), language);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var code = await GetIssuedOtpAsync(phone, OtpPurpose.Registration);
        var sms = Assert.Single(FakeSmsService.SentTo(phone));
        Assert.Equal(SmsTexts.Registration(code, language), sms.Message);
    }

    [Theory]
    [InlineData("ru")]
    [InlineData("uz-Latn")]
    public async Task ForgotPassword_ShouldSendTheCode_InTheAccountLanguage(string language)
    {
        var (userId, phone) = await SeedUserAsync("Passw0rd!");
        await _context.Users.IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Language, language));

        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);

        var code = await GetIssuedOtpAsync(phone, OtpPurpose.PasswordReset);
        var sms = await WaitForSmsAsync(phone);
        Assert.Equal(SmsTexts.PasswordReset(code, language), sms.Message);
    }

    [Fact]
    public async Task ForgotPassword_ShouldSendNothing_ToANumberWithoutAnAccount()
    {
        var phone = NewPhone();

        await _client.PostAsync<ForgotPasswordResponse>("auth/forgot-password", new { phoneNumber = phone }, HttpStatusCode.OK);

        // Nothing is queued for an unknown number; the pause gives a wrongly queued message time to reach the sender.
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        Assert.Empty(FakeSmsService.SentTo(phone));
    }

    /// <summary>The reset SMS leaves through the background queue, after the response.</summary>
    private static async Task<SmsMessage> WaitForSmsAsync(string phone)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var sent = FakeSmsService.SentTo(phone);

            if (sent.Count > 0)
            {
                return Assert.Single(sent);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new TimeoutException($"No SMS reached {phone} within 5 seconds.");
    }
}
