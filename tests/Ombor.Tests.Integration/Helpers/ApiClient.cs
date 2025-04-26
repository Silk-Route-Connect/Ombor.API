using System.Net;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Helpers;

public sealed class ApiClient(HttpClient client, ITestOutputHelper outputHelper)
{
    private static readonly JsonSerializerOptions _jsonPrintOptions = new()
    {
        WriteIndented = true
    };

    public Task GetAsync(string url, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        => SendAsync(HttpMethod.Get, url, expectedStatusCode);

    public Task<TResult> GetAsync<TResult>(string url, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        => SendAsync<TResult>(HttpMethod.Get, url, expectedStatusCode);

    public Task<TResult> PostAsync<TResult>(string url, object content, HttpStatusCode expectedStatusCode = HttpStatusCode.Created)
        => SendAsync<TResult>(HttpMethod.Post, url, expectedStatusCode, content);

    public Task<TResult> PutAsync<TResult>(string url, object content, HttpStatusCode expectedStatusCode = HttpStatusCode.OK)
        => SendAsync<TResult>(HttpMethod.Put, url, expectedStatusCode, content);

    public Task PutAsync(string url, object content, HttpStatusCode expectedStatusCode)
        => SendAsync(HttpMethod.Put, url, expectedStatusCode, content);

    public Task PatchAsJsonAsyncAsync(string url, object content, HttpStatusCode expectedStatusCode)
        => SendAsync(HttpMethod.Patch, url, expectedStatusCode, content);

    public Task<TResult> PatchAsync<TResult>(string url, object content, HttpStatusCode expectedStatusCode = HttpStatusCode.NoContent)
        => SendAsync<TResult>(HttpMethod.Patch, url, expectedStatusCode, content);

    public Task PatchAsync(string url, object content, HttpStatusCode expectedStatusCode = HttpStatusCode.NoContent)
        => SendAsync(HttpMethod.Patch, url, expectedStatusCode, content);

    public Task DeleteAsync(string url, HttpStatusCode expectedStatusCode = HttpStatusCode.NoContent)
        => SendAsync(HttpMethod.Delete, url, expectedStatusCode);

    public Task<TResult> DeleteAsync<TResult>(string url, HttpStatusCode expectedStatusCode = HttpStatusCode.NoContent)
        => SendAsync<TResult>(HttpMethod.Delete, url, expectedStatusCode);

    private async Task<TResult> SendAsync<TResult>(HttpMethod method, string url, HttpStatusCode expectedStatusCode, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = GetStringContent(method, body);
        }

        var response = await client.SendAsync(request);

        var result = await DeserializeAndCheckResponseAsync<TResult>(response, expectedStatusCode);

        return result;
    }

    private async Task SendAsync(HttpMethod method, string url, HttpStatusCode expectedStatusCode, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = GetStringContent(method, body);
        }

        var response = await client.SendAsync(request);

        Assert.NotNull(response);
        Assert.Equal(expectedStatusCode, response.StatusCode);
    }

    private async Task<T> DeserializeAndCheckResponseAsync<T>(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        var stringContent = await response.Content.ReadAsStringAsync();
        WriteOutput(stringContent);

        var result = JsonConvert.DeserializeObject<T>(stringContent);

        Assert.NotNull(result);
        Assert.Equal(expectedStatus, response.StatusCode);

        return result;
    }

    private void WriteOutput(string stringContent)
    {
        string? outputText;

        try
        {
            var jsonContent = JsonDocument.Parse(stringContent);
            outputText = System.Text.Json.JsonSerializer.Serialize(jsonContent, _jsonPrintOptions);
        }
        catch
        {
            outputText = stringContent;
        }

        outputHelper.WriteLine(outputText);
    }

    private static StringContent GetStringContent(HttpMethod method, object content)
    {
        var json = JsonConvert.SerializeObject(content);

        if (method == HttpMethod.Patch)
        {
            return new StringContent(json, Encoding.UTF8, "application/json-patch+json");
        }

        return new StringContent(json, Encoding.UTF8, "application/json");
    }
}
