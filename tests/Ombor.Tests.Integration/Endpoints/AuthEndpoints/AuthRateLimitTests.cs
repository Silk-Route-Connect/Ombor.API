using System.Net;
using Microsoft.Extensions.Configuration;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

/// <summary>
/// The per-IP limiter, on a dedicated host with deliberately low limits (the shared Testing configuration raises
/// them so the rest of the suite is never throttled).
/// </summary>
public sealed class AuthRateLimitTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : AuthTestsBase(factory, output)
{
    [Fact]
    public async Task AnonymousAuthEndpoints_Return429ProblemDetails_PastThePerIpLimit()
    {
        using var limitedHost = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AuthSecuritySettings:AuthPermitLimit"] = "2",
                    ["AuthSecuritySettings:SmsPermitLimit"] = "1",
                })));
        using var client = limitedHost.CreateDefaultClient(new Uri("https://localhost/api/"));

        var login = new { phoneNumber = NewPhone(), password = "Passw0rd!" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, HttpMethod.Post, "auth/login", login)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await SendAsync(client, HttpMethod.Post, "auth/login", login)).Status);

        var throttled = await SendAsync(client, HttpMethod.Post, "auth/login", login);

        Assert.Equal(HttpStatusCode.TooManyRequests, throttled.Status);
        Assert.Equal(ErrorCodes.RateLimited, throttled.Code);
        Assert.NotNull(throttled.RetryAfter);
        Assert.InRange((int)throttled.Body["params"]!["retryAfterSeconds"]!, 1, 60);

        // The SMS policy is a separate, stricter bucket: different phones, same IP.
        var firstSms = await SendAsync(client, HttpMethod.Post, "auth/forgot-password", new { phoneNumber = NewPhone() });
        var secondSms = await SendAsync(client, HttpMethod.Post, "auth/forgot-password", new { phoneNumber = NewPhone() });

        Assert.Equal(HttpStatusCode.OK, firstSms.Status);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondSms.Status);
        Assert.Equal(ErrorCodes.RateLimited, secondSms.Code);
    }
}
