using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Domain.Exceptions;

namespace Ombor.Application.Services;

internal sealed class LoginThrottle(IRedisService redisService, IOptions<AuthSecuritySettings> options) : ILoginThrottle
{
    private const string FailuresKeyPattern = "login:failures:{0}";

    private readonly AuthSecuritySettings _settings = options.Value;

    public async Task BeginAttemptAsync(string phoneNumber)
    {
        var attempts = await redisService.IncrementAsync(
            string.Format(FailuresKeyPattern, phoneNumber),
            TimeSpan.FromMinutes(_settings.FailedLoginWindowMinutes));

        if (attempts.Count > _settings.MaxFailedLogins)
        {
            throw new TooManyRequestsException(attempts.RemainingWindow, "Too many failed sign-in attempts. Try again later.");
        }
    }

    public Task ResetAsync(string phoneNumber) =>
        redisService.RemoveAsync(string.Format(FailuresKeyPattern, phoneNumber));
}
