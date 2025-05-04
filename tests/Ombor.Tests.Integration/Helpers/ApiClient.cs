using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace Ombor.Tests.Integration.Helpers;

public sealed class ApiClient(HttpClient client)
{
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

        return await DeserializeAndCheckResponseAsync<TResult>(response, expectedStatusCode);
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

    private static async Task<T> DeserializeAndCheckResponseAsync<T>(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        var stringContent = await response.Content.ReadAsStringAsync();

        var result = JsonConvert.DeserializeObject<T>(stringContent);

        Assert.NotNull(result);
        Assert.Equal(expectedStatus, response.StatusCode);

        return result;
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
