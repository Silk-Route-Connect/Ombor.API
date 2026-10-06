using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Tests.Integration.Endpoints.Transactions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.Concurrency;

/// <summary>
/// Fires the same money or stock write many times at once (backend-7), the way several cashiers press «Провести»
/// together, so each test can assert the invariant the organization write lock protects. Every request gets its own
/// scope and DbContext on the test server, exactly as parallel HTTP requests do in production.
/// </summary>
public abstract class ConcurrencyTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    protected const int ParallelRequests = 10;

    /// <summary>Builds every request first, then sends them all at once and collects status + body.</summary>
    protected async Task<ParallelOutcome[]> FireAsync(Func<int, HttpRequestMessage> buildRequest, int count = ParallelRequests)
    {
        using var client = CreateClient(_factory);
        var requests = Enumerable.Range(0, count).Select(buildRequest).ToArray();

        var responses = await Task.WhenAll(requests.Select(request => client.SendAsync(request)));

        return await Task.WhenAll(responses.Select(async response =>
            new ParallelOutcome(response.StatusCode, await response.Content.ReadAsStringAsync())));
    }

    protected static HttpClient CreateClient(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> host)
    {
        var client = host.CreateDefaultClient(new Uri("https://localhost/api/"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        return client;
    }

    protected static HttpRequestMessage Json(string url, object body) =>
        new(HttpMethod.Post, url) { Content = JsonContent.Create(body) };

    protected static HttpRequestMessage Form(string url, HttpContent content) =>
        new(HttpMethod.Post, url) { Content = content };

    /// <summary>
    /// Exactly <paramref name="succeeded"/> requests passed with <paramref name="success"/>; every other one was refused
    /// with a 4xx carrying <paramref name="refusedCode"/> — never a 500, never a silent extra success.
    /// </summary>
    protected static void AssertOutcomes(
        ParallelOutcome[] outcomes,
        int succeeded,
        HttpStatusCode success,
        HttpStatusCode refusedStatus,
        string? refusedCode)
    {
        Assert.All(outcomes, o => Assert.True(
            o.Status == success || (o.Status == refusedStatus && o.Code == refusedCode),
            $"Unexpected {(int)o.Status} {o.Code}: {o.Body}"));
        Assert.Equal(succeeded, outcomes.Count(o => o.Status == success));
    }

    protected Task<decimal> StockOfAsync(int warehouseId, int productId) =>
        _context.WarehouseItems
            .AsNoTracking()
            .Where(i => i.WarehouseId == warehouseId && i.ProductId == productId)
            .Select(i => i.Quantity)
            .FirstOrDefaultAsync();

    protected sealed record ParallelOutcome(HttpStatusCode Status, string Body)
    {
        public string? Code => Body.StartsWith('{') ? (string?)JObject.Parse(Body)["code"] : null;
    }
}
