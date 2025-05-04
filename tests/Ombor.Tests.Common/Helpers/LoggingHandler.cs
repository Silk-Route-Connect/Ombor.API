using System.Text.Json;
using Xunit.Abstractions;

namespace Ombor.Tests.Common.Helpers;

public sealed class LoggingHandler(ITestOutputHelper outputHelper) : DelegatingHandler
{
    private static readonly JsonSerializerOptions _jsonPrintOptions = new()
    {
        WriteIndented = true
    };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        outputHelper.WriteLine($"Sending {request.Method} request to {request.RequestUri}");
        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsStringAsync(cancellationToken);
            WriteContent(content);
        }

        var response = await base.SendAsync(request, cancellationToken);

        outputHelper.WriteLine($"Received {(int)response.StatusCode} {response.ReasonPhrase}");
        if (response.Content is not null)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            WriteContent(content);
        }

        return response;
    }

    private void WriteContent(string content)
    {
        string? outputText;

        try
        {
            var jsonContent = JsonDocument.Parse(content);
            outputText = JsonSerializer.Serialize(jsonContent, _jsonPrintOptions);
        }
        catch
        {
            outputText = content;
        }

        outputHelper.WriteLine(outputText);
    }
}
