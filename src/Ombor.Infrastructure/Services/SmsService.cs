using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;

namespace Ombor.Infrastructure.Services;

internal sealed class SmsService(
    IRequestValidator validator,
    HttpClient client,
    IOptions<SmsSettings> smsSettings) : ISmsService
{
    private readonly SmsSettings options = smsSettings.Value;

    public async Task SendMessageAsync(SmsMessage message)
    {
        await validator.ValidateAndThrowAsync(message);

        var apiUrl = options.ApiUrl;
        var token = options.Token;
        var from = options.FromNumber;

        if (string.IsNullOrWhiteSpace(apiUrl) ||
           string.IsNullOrWhiteSpace(token) ||
           string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("SMS configuration is missing required values.");
        }

        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var _))
        {
            throw new InvalidOperationException("SMS provider Api URL is invalid.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            mobile_phone = message.ToNumber,
            message = message.Message,
            from = from
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        using var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException($"SMS provider request failed with status {(int)response.StatusCode} {response.ReasonPhrase}. Response: {error}");
        }
    }
}