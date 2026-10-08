using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Responses.Notification;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.NotificationEndpoints;

/// <summary>
/// GET /api/notifications — alerts computed from the ledger on every read. The shared test database holds other tests'
/// rows, so each test plants its own records and asserts the change they make (and, for the top-10 list, makes its
/// record the most pressing one).
/// </summary>
public abstract class NotificationTestsBase(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    protected const string Route = "notifications";
    protected const int ForeignOrganizationId = 2;

    protected override string GetUrl() => Route;

    protected override string GetUrl(int id) => $"{Route}/{id}";

    protected async Task<NotificationDto?> GetAsync(NotificationKind kind) =>
        (await _client.GetAsync<NotificationDto[]>(Route)).SingleOrDefault(n => n.Kind == kind);

    private protected ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
