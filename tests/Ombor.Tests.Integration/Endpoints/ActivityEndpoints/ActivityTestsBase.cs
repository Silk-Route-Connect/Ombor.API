using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Endpoints.Transactions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ActivityEndpoints;

/// <summary>
/// Activity Log tests: writes go through the API (so the audit interceptor records them as the signed-in user) and
/// the log is read back through <c>GET /api/activity</c>. Records a test only needs to exist are planted through the
/// test context, which records no audit rows.
/// </summary>
public abstract class ActivityTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : TransactionsTestsBase(factory, output)
{
    /// <summary>The signed-in user of the test host (the <c>AuthHandler</c> claim).</summary>
    protected const int CurrentUserId = 1;

    protected const string ActivityRoute = "activity";

    protected override string GetUrl() => ActivityRoute;

    protected override string GetUrl(int id) => $"{ActivityRoute}/{id}";

    protected Task<JObject> GetActivityAsync(string query) => _client.GetAsync<JObject>($"{ActivityRoute}?{query}");

    /// <summary>The newest operation in one record's history.</summary>
    protected async Task<JObject> LatestAsync(string entityKind, int entityId)
    {
        var page = await GetActivityAsync($"entityKind={entityKind}&entityId={entityId}");

        return (JObject)page["items"]!.First!;
    }

    protected static JArray Items(JObject page) => (JArray)page["items"]!;

    protected static JObject ChangeOf(JObject item, string entityKind, int? entityId = null) =>
        (JObject)item["changes"]!.First(c =>
            (string)c["entityKind"]! == entityKind && (entityId is null || (int)c["entityId"]! == entityId));

    protected static JObject FieldOf(JObject change, string field) =>
        (JObject)change["fields"]!.Single(f => (string)f["field"]! == field);

    protected static bool HasField(JObject change, string field) =>
        change["fields"]!.Any(f => (string)f["field"]! == field);

    /// <summary>A context pinned to another organization, recording audit rows like the API does.</summary>
    private protected ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
