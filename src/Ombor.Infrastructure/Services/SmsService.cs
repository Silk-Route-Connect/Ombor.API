using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;
using Ombor.Domain.Exceptions;

namespace Ombor.Infrastructure.Services;

internal sealed class SmsService(
    IRequestValidator validator,
    HttpClient client,
    IEskizTokenProvider tokenProvider,
    IOptions<SmsSettings> smsSettings) : ISmsService
{
    private readonly SmsSettings options = smsSettings.Value;

    public async Task SendMessageAsync(SmsMessage message)
    {
        await validator.ValidateAndThrowAsync(message);

        if (string.IsNullOrWhiteSpace(options.ApiUrl) || string.IsNullOrWhiteSpace(options.FromNumber))
        {
            throw new InvalidOperationException("SMS configuration is missing required values.");
        }

        if (!Uri.TryCreate(options.ApiUrl, UriKind.Absolute, out var _))
        {
            throw new InvalidOperationException("SMS provider Api URL is invalid.");
        }

        var token = await tokenProvider.GetTokenAsync();
        var response = await SendAsync(message, token);

        // An expired or revoked token: renew it once and resend (the account login issues a fresh one).
        if (response.StatusCode == HttpStatusCode.Unauthorized && tokenProvider.CanRenew)
        {
            response.Dispose();
            token = await tokenProvider.GetTokenAsync(rejectedToken: token);
            response = await SendAsync(message, token);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();

                throw new SmsDeliveryException($"SMS provider request failed with status {(int)response.StatusCode} {response.ReasonPhrase}. Response: {error}");
            }
        }
    }

    private async Task<HttpResponseMessage> SendAsync(SmsMessage message, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            mobile_phone = message.ToNumber,
            message = message.Message,
            from = options.FromNumber,
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            return await client.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            // Network/DNS failure reaching the provider — surface as a retryable outage, not a 500.
            throw new SmsDeliveryException("SMS provider is temporarily unavailable.", ex);
        }
    }
}
