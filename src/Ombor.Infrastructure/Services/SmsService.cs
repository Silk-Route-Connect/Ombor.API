using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;

namespace Ombor.Infrastructure.Services;

internal sealed class SmsService(IConfiguration configuration, HttpClient client) : ISmsService
{
    public async Task SendMessageAsync(SmsMessage message)
    {
        ArgumentNullException.ThrowIfNull(nameof(message));

        if (string.IsNullOrWhiteSpace(message.ToNumber))
        {
            throw new ArgumentException("Phone number cannot be empty.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Message))
        {
            throw new ArgumentException("Message cannot be empty.", nameof(message));
        }

        var apiUrl = configuration["SmsConfigurations:ApiUrl"];
        var token = configuration["SmsConfigurations:Token"];
        var from = configuration["SmsConfigurations:From"];

        if (string.IsNullOrWhiteSpace(apiUrl) ||
           string.IsNullOrWhiteSpace(token) ||
           string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("SMS configuration is missing required values.");
        }

        if (!Uri.TryCreate(apiUrl, UriKind.Absolute, out var uriResult))
        {
            throw new InvalidOperationException("SMS provider Api URL is invalid.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration["SmsConfigurations:Token"]);

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
            throw new HttpRequestException($"SMS provider request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
    }
}