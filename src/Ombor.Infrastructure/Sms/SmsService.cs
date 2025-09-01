using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Ombor.Application.Interfaces;
using Ombor.Application.Models;

namespace Ombor.Infrastructure.Sms;

internal sealed class SmsService : ISmsService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _client;

    public SmsService(IConfiguration configuration, HttpClient client)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }
    public async Task SendMessageAsync(SmsMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.ToNumber))
            throw new ArgumentException("Phone number cannot be empty.", nameof(message));

        if (string.IsNullOrWhiteSpace(message.Message))
            throw new ArgumentException("Message cannot be empty.", nameof(message));

        using var request = new HttpRequestMessage(HttpMethod.Post, _configuration["SmsConfigurations:ApiUrl"]);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuration["SmsConfigurations:Token"]);

        var payload = new
        {
            mobile_phone = message.ToNumber,
            message = message.Message,
            from = _configuration["SmsConfigurations:From"]
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Eskiz SMS error: {response.StatusCode}, {body}");
    }

}
