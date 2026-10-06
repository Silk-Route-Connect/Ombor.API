using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class OtpCodeProvider(IRedisService redisService, IOptions<OtpSettings> options) : IOtpCodeProvider
{
    private const string OtpKeyPattern = "otp:{0}:{1}";
    private const string AttemptsKeyPattern = "otp:attempts:{0}:{1}";
    private const string CooldownKeyPattern = "otp:cooldown:{0}:{1}";
    private const string DailyKeyPattern = "otp:daily:{0}:{1}";
    private const string RegisterRequestKeyPattern = "reg:req:{0}";

    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(OtpSettings.CodeLifetimeMinutes);
    private static readonly TimeSpan DailyWindow = TimeSpan.FromDays(1);

    private readonly OtpSettings _settings = options.Value;

    public int CodeLength => _settings.CodeLength;

    public int ResendAfterSeconds => _settings.ResendCooldownSeconds;

    public async Task EnsureCanSendAsync(string phoneNumber, OtpPurpose purpose)
    {
        if (_settings.ResendCooldownSeconds > 0)
        {
            var recent = await redisService.IncrementAsync(
                Key(CooldownKeyPattern, phoneNumber, purpose),
                TimeSpan.FromSeconds(_settings.ResendCooldownSeconds));

            if (recent.Count > 1)
            {
                throw new TooManyRequestsException(recent.RemainingWindow, "A code was sent recently. Wait before requesting another.");
            }
        }

        var today = await redisService.IncrementAsync(Key(DailyKeyPattern, phoneNumber, purpose), DailyWindow);

        if (today.Count > _settings.DailySendLimit)
        {
            throw new TooManyRequestsException(today.RemainingWindow, "Too many codes requested for this number today.");
        }
    }

    public async Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose)
    {
        // Inclusive lower / exclusive upper bound: every code has exactly CodeLength digits, no leading zero.
        var lowest = (int)Math.Pow(10, _settings.CodeLength - 1);
        var code = RandomNumberGenerator.GetInt32(lowest, lowest * 10).ToString(CultureInfo.InvariantCulture);

        var otpData = new OtpCode(phoneNumber, code, purpose, DateTime.UtcNow.Add(CodeLifetime));

        await redisService.SetAsync(Key(OtpKeyPattern, phoneNumber, purpose), otpData, CodeLifetime);
        await redisService.RemoveAsync(Key(AttemptsKeyPattern, phoneNumber, purpose));

        return code;
    }

    public Task<OtpCode?> GetOtpAsync(string phoneNumber, OtpPurpose purpose) =>
        redisService.GetAsync<OtpCode>(Key(OtpKeyPattern, phoneNumber, purpose));

    public async Task<OtpCheckResult> VerifyOtpAsync(string phoneNumber, OtpPurpose purpose, string code)
    {
        var attemptsKey = Key(AttemptsKeyPattern, phoneNumber, purpose);
        var attempts = await redisService.IncrementAsync(attemptsKey, CodeLifetime);

        if (attempts.Count > _settings.MaxVerifyAttempts)
        {
            await RemoveOtpAsync(phoneNumber, purpose);
            return OtpCheckResult.TooManyAttempts;
        }

        var otp = await GetOtpAsync(phoneNumber, purpose);

        if (otp is null || DateTime.UtcNow > otp.ExpiredAt)
        {
            return OtpCheckResult.Expired;
        }

        if (!CodesMatch(otp.Code, code))
        {
            if (attempts.Count < _settings.MaxVerifyAttempts)
            {
                return OtpCheckResult.Invalid;
            }

            await RemoveOtpAsync(phoneNumber, purpose);
            return OtpCheckResult.TooManyAttempts;
        }

        // The reset flow presents the same correct code twice (verify-reset-code, then reset-password); a correct
        // code proves possession, so it clears the count instead of eating into the wrong-guess budget.
        await redisService.RemoveAsync(attemptsKey);

        return OtpCheckResult.Valid;
    }

    public Task<RegisterRequest?> GetRegisterRequestAsync(string phoneNumber) =>
        redisService.GetAsync<RegisterRequest>(string.Format(RegisterRequestKeyPattern, phoneNumber));

    public Task SetRegisterRequestAsync(RegisterRequest request, TimeSpan lifetime) =>
        redisService.SetAsync<RegisterRequest>(string.Format(RegisterRequestKeyPattern, request.PhoneNumber), request, lifetime);

    public Task RemoveOtpAsync(string phoneNumber, OtpPurpose purpose) =>
        redisService.RemoveAsync(Key(OtpKeyPattern, phoneNumber, purpose));

    public Task RemoveRegisterRequestAsync(string phoneNumber) =>
        redisService.RemoveAsync(string.Format(RegisterRequestKeyPattern, phoneNumber));

    private static string Key(string pattern, string phoneNumber, OtpPurpose purpose) =>
        string.Format(CultureInfo.InvariantCulture, pattern, phoneNumber, purpose);

    private static bool CodesMatch(string expected, string? actual) =>
        actual is not null
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(actual.Trim()));
}
