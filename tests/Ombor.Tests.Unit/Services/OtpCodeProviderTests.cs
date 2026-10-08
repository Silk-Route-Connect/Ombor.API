using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Models;
using Ombor.Application.Services;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;
using CacheService = Ombor.Infrastructure.Services.MemoryCache;

namespace Ombor.Tests.Unit.Services;

public sealed class OtpCodeProviderTests
{
    private const string Phone = "+998901234567";

    private readonly OtpCodeProvider _provider = CreateProvider(new OtpSettings());

    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(8)]
    public async Task GenerateOtpAsync_IssuesCodesOfTheConfiguredLength_WithoutLeadingZero(int length)
    {
        var provider = CreateProvider(new OtpSettings { CodeLength = length });

        for (var i = 0; i < 50; i++)
        {
            var code = await provider.GenerateOtpAsync(Phone, OtpPurpose.PasswordReset);

            Assert.Equal(length, code.Length);
            Assert.All(code, c => Assert.True(char.IsAsciiDigit(c)));
            Assert.NotEqual('0', code[0]);
        }
    }

    [Fact]
    public async Task VerifyOtpAsync_InvalidatesTheCode_AfterFiveWrongGuesses()
    {
        var code = await _provider.GenerateOtpAsync(Phone, OtpPurpose.PasswordReset);
        var wrong = WrongCodeFor(code);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            Assert.Equal(OtpCheckResult.Invalid, await _provider.VerifyOtpAsync(Phone, OtpPurpose.PasswordReset, wrong));
        }

        Assert.Equal(OtpCheckResult.TooManyAttempts, await _provider.VerifyOtpAsync(Phone, OtpPurpose.PasswordReset, wrong));

        // The 6th guess fails even with the right code: the code is gone.
        Assert.Equal(OtpCheckResult.TooManyAttempts, await _provider.VerifyOtpAsync(Phone, OtpPurpose.PasswordReset, code));
        Assert.Null(await _provider.GetOtpAsync(Phone, OtpPurpose.PasswordReset));
    }

    [Fact]
    public async Task VerifyOtpAsync_CorrectCode_ClearsTheWrongGuessCount()
    {
        var code = await _provider.GenerateOtpAsync(Phone, OtpPurpose.PasswordReset);
        var wrong = WrongCodeFor(code);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await _provider.VerifyOtpAsync(Phone, OtpPurpose.PasswordReset, wrong);
        }

        // verify-reset-code then reset-password present the same correct code twice; both must pass.
        Assert.Equal(OtpCheckResult.Valid, await _provider.VerifyOtpAsync(Phone, OtpPurpose.PasswordReset, code));
        Assert.Equal(OtpCheckResult.Valid, await _provider.VerifyOtpAsync(Phone, OtpPurpose.PasswordReset, code));
    }

    [Fact]
    public async Task GenerateOtpAsync_ResetsTheWrongGuessCount()
    {
        var first = await _provider.GenerateOtpAsync(Phone, OtpPurpose.PasswordReset);

        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await _provider.VerifyOtpAsync(Phone, OtpPurpose.PasswordReset, WrongCodeFor(first));
        }

        var second = await _provider.GenerateOtpAsync(Phone, OtpPurpose.PasswordReset);

        Assert.Equal(OtpCheckResult.Valid, await _provider.VerifyOtpAsync(Phone, OtpPurpose.PasswordReset, second));
    }

    [Fact]
    public async Task VerifyOtpAsync_ReportsExpired_WhenNoCodeWasIssued()
    {
        Assert.Equal(OtpCheckResult.Expired, await _provider.VerifyOtpAsync(Phone, OtpPurpose.Registration, "123456"));
    }

    [Fact]
    public async Task EnsureCanSendAsync_EnforcesTheResendCooldown_PerPhoneAndPurpose()
    {
        await _provider.EnsureCanSendAsync(Phone, OtpPurpose.PasswordReset);

        var throttled = await Assert.ThrowsAsync<TooManyRequestsException>(
            () => _provider.EnsureCanSendAsync(Phone, OtpPurpose.PasswordReset));

        Assert.InRange(throttled.RetryAfterSeconds, 1, 60);
        Assert.Equal(ErrorCodes.RateLimited, throttled.Code);

        // Another purpose or another phone has its own budget.
        await _provider.EnsureCanSendAsync(Phone, OtpPurpose.Registration);
        await _provider.EnsureCanSendAsync("+998901234568", OtpPurpose.PasswordReset);
    }

    [Fact]
    public async Task EnsureCanSendAsync_EnforcesTheDailyCap()
    {
        var provider = CreateProvider(new OtpSettings { ResendCooldownSeconds = 0, DailySendLimit = 3 });

        for (var send = 1; send <= 3; send++)
        {
            await provider.EnsureCanSendAsync(Phone, OtpPurpose.Registration);
        }

        await Assert.ThrowsAsync<TooManyRequestsException>(() => provider.EnsureCanSendAsync(Phone, OtpPurpose.Registration));
    }

    private static OtpCodeProvider CreateProvider(OtpSettings settings) =>
        new(new CacheService(new MemoryCache(new MemoryCacheOptions())), Options.Create(settings));

    private static string WrongCodeFor(string code) =>
        code[0] == '1' ? "2" + code[1..] : "1" + code[1..];
}
