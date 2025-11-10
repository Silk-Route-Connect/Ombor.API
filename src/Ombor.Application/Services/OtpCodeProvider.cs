using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Contracts.Requests.Auth;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal class OtpCodeProvider(IRedisService redisService) : IOtpCodeProvider
{
    private const string OtpKeyPattern = "otp:{0}:{1}";
    private const string RegisterRequestKeyPattern = "reg:req:{0}";

    public async Task<string> GenerateOtpAsync(string phoneNumber, OtpPurpose purpose, int lifetimeInMinutes)
    {
        // TODO: Uncomment the logic for code generation when released to production!
        var code = "1234"; // RandomNumberGenerator.GetInt32(1000, 9999).ToString();

        var ttl = TimeSpan.FromMinutes(lifetimeInMinutes);
        var otpData = new OtpCode(
            phoneNumber,
            code,
            purpose,
            DateTime.UtcNow.Add(ttl));

        var key = string.Format(OtpKeyPattern, otpData.PhoneNumber, otpData.Purpose);
        await redisService.SetAsync(key, otpData, ttl);

        return code;
    }

    public Task<OtpCode?> GetOtpAsync(string phoneNumber, OtpPurpose purpose) =>
        redisService.GetAsync<OtpCode>(string.Format(OtpKeyPattern, phoneNumber, purpose));

    public Task<RegisterRequest?> GetRegisterRequestAsync(string phoneNumber) =>
        redisService.GetAsync<RegisterRequest>(string.Format(RegisterRequestKeyPattern, phoneNumber));

    public Task SetRegisterRequestAsync(RegisterRequest request, TimeSpan lifetime) =>
        redisService.SetAsync<RegisterRequest>(string.Format(RegisterRequestKeyPattern, request.PhoneNumber), request, lifetime);

    public Task RemoveOtpAsync(string phoneNumber, OtpPurpose purpose) =>
        redisService.RemoveAsync(string.Format(OtpKeyPattern, phoneNumber, purpose));

    public Task RemoveRegisterRequestAsync(string phoneNumber) =>
        redisService.RemoveAsync(string.Format(RegisterRequestKeyPattern, phoneNumber));
}
