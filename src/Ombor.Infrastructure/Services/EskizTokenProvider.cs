using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Domain.Exceptions;

namespace Ombor.Infrastructure.Services;

/// <summary>Supplies the bearer token for Eskiz SMS calls.</summary>
internal interface IEskizTokenProvider
{
    /// <summary>Whether a rejected token can be replaced (account credentials are configured).</summary>
    bool CanRenew { get; }

    /// <summary>
    /// The token to send. Pass the token Eskiz just rejected (401) to force a fresh login; a token another request
    /// already renewed is reused instead of logging in twice.
    /// </summary>
    Task<string> GetTokenAsync(string? rejectedToken = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Eskiz tokens expire 30 days after issue, so a fixed token silently breaks registration and password reset every
/// month. With <see cref="SmsSettings.Email"/> + <see cref="SmsSettings.Password"/> configured this logs in, caches
/// the token for the process, and logs in again a day before the token's <c>exp</c> or when Eskiz rejects it. The
/// fixed <see cref="SmsSettings.Token"/> stays the fallback when no account is configured or the login fails.
/// Singleton: the cached token is shared by every request.
/// </summary>
internal sealed class EskizTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<SmsSettings> smsSettings,
    ILogger<EskizTokenProvider> logger) : IEskizTokenProvider
{
    private static readonly TimeSpan RenewBeforeExpiry = TimeSpan.FromDays(1);

    private readonly SmsSettings _settings = smsSettings.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);

    // One immutable snapshot swapped atomically, so a reader outside the gate never sees a token with another's expiry.
    private volatile CachedToken? _cached;

    public bool CanRenew => _settings.HasAccountCredentials;

    public async Task<string> GetTokenAsync(string? rejectedToken = null, CancellationToken cancellationToken = default)
    {
        if (!CanRenew)
        {
            return StaticTokenOrThrow();
        }

        if (IsUsable(_cached, rejectedToken, out var cachedToken))
        {
            return cachedToken;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            // Another request may have renewed the token while this one waited.
            if (IsUsable(_cached, rejectedToken, out cachedToken))
            {
                return cachedToken;
            }

            try
            {
                var token = await LoginAsync(cancellationToken);
                _cached = new CachedToken(token, ReadExpiry(token));
                return token;
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or SmsDeliveryException or TaskCanceledException)
            {
                logger.LogError(ex, "Eskiz login failed; falling back to the configured SMS token if there is one.");
                _cached = null;
                return StaticTokenOrThrow(ex);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private static bool IsUsable(CachedToken? cached, string? rejectedToken, out string token)
    {
        token = cached?.Token ?? string.Empty;

        return cached is not null
            && cached.Token != rejectedToken
            && (cached.ExpiresAt is null || cached.ExpiresAt.Value - RenewBeforeExpiry > DateTimeOffset.UtcNow);
    }

    private string StaticTokenOrThrow(Exception? cause = null) =>
        string.IsNullOrWhiteSpace(_settings.Token)
            ? throw new SmsDeliveryException("No Eskiz token is available: the account login failed and no fallback token is configured.", cause)
            : _settings.Token;

    private async Task<string> LoginAsync(CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(_settings.Email!), "email" },
            { new StringContent(_settings.Password!), "password" },
        };

        using var client = httpClientFactory.CreateClient(nameof(EskizTokenProvider));
        using var response = await client.PostAsync(_settings.AuthUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new SmsDeliveryException($"Eskiz login failed with status {(int)response.StatusCode}.");
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(body, cancellationToken: cancellationToken);

        // Eskiz answers {"message":"token_generated","data":{"token":"..."},"token_type":"bearer"}.
        if (json.RootElement.TryGetProperty("data", out var data)
            && data.TryGetProperty("token", out var tokenElement)
            && tokenElement.GetString() is { Length: > 0 } token)
        {
            return token;
        }

        throw new SmsDeliveryException("Eskiz login response carried no token.");
    }

    private sealed record CachedToken(string Token, DateTimeOffset? ExpiresAt);

    /// <summary>The token's <c>exp</c> claim, or null when it is not a readable JWT (then only a 401 renews it).</summary>
    internal static DateTimeOffset? ReadExpiry(string token)
    {
        var parts = token.Split('.');

        if (parts.Length != 3)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - (payload.Length % 4)) % 4), '=');

            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));

            return json.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds)
                : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
