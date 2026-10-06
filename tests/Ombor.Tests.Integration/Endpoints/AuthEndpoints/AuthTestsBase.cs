using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Tests.Integration.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.AuthEndpoints;

public abstract class AuthTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected const string RefreshTokenCookie = "ombor.refreshToken";
    protected const string LanguageHeader = "X-Ombor-Language";

    protected override string GetUrl() => "auth";
    protected override string GetUrl(int id) => $"auth/{id}";

    // A valid Uzbek mobile: +998 followed by a 9xxxxxxxx local part (rule the phone validator enforces).
    protected static string NewPhone() =>
        $"+9989{Math.Abs(Guid.NewGuid().GetHashCode()) % 100_000_000:D8}";

    /// <summary>Seeds an active, phone-confirmed user with a known password and returns its id + phone.</summary>
    protected async Task<(int userId, string phone)> SeedUserAsync(string password)
    {
        var phone = NewPhone();

        using var scope = _factory.Services.CreateScope();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var hash = hasher.HashPassword(password);

        var user = new User
        {
            FirstName = "Reset",
            LastName = "User",
            PhoneNumber = phone,
            PasswordHash = hash.Hash,
            PasswordSalt = hash.Salt,
            IsPhoneNumberConfirmed = true,
            Organization = null!,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (user.Id, phone);
    }

    /// <summary>
    /// Reads the OTP the server actually issued (via the shared in-memory store), instead of assuming a fixed
    /// stub — the provider mints a random code (<c>OtpCodeProvider.GenerateOtpAsync</c>).
    /// </summary>
    protected async Task<string> GetIssuedOtpAsync(string phone, OtpPurpose purpose)
    {
        using var scope = _factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IOtpCodeProvider>();

        var otp = await provider.GetOtpAsync(phone, purpose);
        Assert.NotNull(otp);

        return otp!.Code;
    }

    /// <summary>
    /// A client with no default auth header and no cookie container, for asserting raw status, headers and the
    /// error body (<c>code</c>/<c>params</c> extension members the typed client cannot see).
    /// </summary>
    protected HttpClient CreateRawClient() => _factory.CreateDefaultClient(new Uri("https://localhost/api/"));

    protected static async Task<RawResponse> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object? body = null,
        string? bearerToken = null,
        string? refreshTokenCookie = null,
        string? language = null)
    {
        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        }

        if (bearerToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        if (refreshTokenCookie is not null)
        {
            // The server writes the cookie URL-encoded and decodes it on read.
            request.Headers.Add("Cookie", $"{RefreshTokenCookie}={Uri.EscapeDataString(refreshTokenCookie)}");
        }

        if (language is not null)
        {
            request.Headers.Add(LanguageHeader, language);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        // Error bodies are objects; a successful list read is an array and simply yields an empty Body.
        var json = string.IsNullOrWhiteSpace(text) ? null : JToken.Parse(text);

        return new RawResponse(
            response.StatusCode,
            text,
            json as JObject ?? new JObject(),
            response.Headers.RetryAfter?.Delta,
            RefreshTokenCookieOf(response));
    }

    /// <summary>The refresh token the response set in the httpOnly cookie — its only carrier (backend-12).</summary>
    private static string? RefreshTokenCookieOf(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var prefix = $"{RefreshTokenCookie}=";
        var cookie = cookies.FirstOrDefault(c => c.StartsWith(prefix, StringComparison.Ordinal));
        var value = cookie?[prefix.Length..].Split(';')[0];

        return string.IsNullOrEmpty(value) ? null : Uri.UnescapeDataString(value);
    }

    protected async Task<RawResponse> PostRawAsync(string url, object body, string? language = null)
    {
        using var client = CreateRawClient();

        return await SendAsync(client, HttpMethod.Post, url, body, language: language);
    }

    /// <summary>Status, raw text, parsed JSON body, the <c>Retry-After</c> delta and the refresh-token cookie of one response.</summary>
    protected sealed record RawResponse(
        System.Net.HttpStatusCode Status, string Text, JObject Body, TimeSpan? RetryAfter, string? RefreshTokenCookie)
    {
        public string? Code => (string?)Body["code"];
    }
}
