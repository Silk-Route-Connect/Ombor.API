using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence.DataFixes;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ActivityEndpoints;

/// <summary>
/// Audit rows recorded before operations existed: the backfill groups them by user and second and links payment
/// parts to their payment, and the Activity Log reads them like new rows (enum numbers shown as names).
/// </summary>
public sealed class AuditOperationBackfillTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ActivityTestsBase(factory, output)
{
    private static readonly DateTimeOffset LegacySecond = new(2020, 1, 1, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Backfill_GroupsLegacyRowsByUserAndSecond_AndIsIdempotent()
    {
        var sameSecond = Legacy(nameof(Category), 1, LegacySecond.AddMilliseconds(100), CurrentUserId);
        var sameSecondToo = Legacy(nameof(Category), 2, LegacySecond.AddMilliseconds(800), CurrentUserId);
        var nextSecond = Legacy(nameof(Category), 3, LegacySecond.AddSeconds(1), CurrentUserId);
        var system = Legacy(nameof(Category), 4, LegacySecond.AddMilliseconds(500), userId: null);
        _context.AuditEntries.AddRange(sameSecond, sameSecondToo, nextSecond, system);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(AuditOperationBackfill.Sql);
        var secondRun = await _context.Database.ExecuteSqlRawAsync(AuditOperationBackfill.Sql);

        var ids = new[] { sameSecond.Id, sameSecondToo.Id, nextSecond.Id, system.Id };
        var rows = await _context.AuditEntries.AsNoTracking().Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id);
        Assert.All(rows.Values, row => Assert.NotNull(row.OperationId));
        Assert.Equal(rows[sameSecond.Id].OperationId, rows[sameSecondToo.Id].OperationId);
        Assert.NotEqual(rows[sameSecond.Id].OperationId, rows[nextSecond.Id].OperationId);
        Assert.NotEqual(rows[sameSecond.Id].OperationId, rows[system.Id].OperationId);
        Assert.Equal(0, secondRun);

        var operation = await _client.GetAsync<JObject>($"{ActivityRoute}/{rows[sameSecond.Id].OperationId}");
        Assert.Equal(2, (int)operation["changeCount"]!);
        Assert.Equal(CurrentUserId, (int)operation["actor"]!["id"]!);
    }

    [Fact]
    public async Task Backfill_LinksLegacyPaymentPartsToTheirPayment()
    {
        var partnerId = await CreatePartnerAsync();
        var paymentId = await CreateAdvancePaymentAsync(partnerId, 5_000m);
        var componentId = await _context.PaymentComponents.Where(c => c.PaymentId == paymentId).Select(c => c.Id).SingleAsync();
        var legacy = Legacy(nameof(PaymentComponent), componentId, LegacySecond.AddMinutes(1), CurrentUserId);
        _context.AuditEntries.Add(legacy);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync(AuditOperationBackfill.Sql);

        var row = await _context.AuditEntries.AsNoTracking().SingleAsync(a => a.Id == legacy.Id);
        Assert.Equal(nameof(Payment), row.ParentEntityType);
        Assert.Equal(paymentId, row.ParentEntityId);

        var history = await GetActivityAsync($"entityKind=Payment&entityId={paymentId}");
        Assert.Contains(Items(history), i => (string)i["operationId"]! == row.OperationId.ToString());
    }

    [Fact]
    public async Task LegacyEnumNumbers_AreServedAsNames()
    {
        var productId = await CreateProductAsync();
        var legacy = Legacy(nameof(Product), productId, LegacySecond.AddMinutes(2), CurrentUserId, AuditAction.Updated);
        legacy.OldValues = """{"Measurement":4,"Name":"Same"}""";
        legacy.NewValues = """{"Measurement":2,"Name":"Same"}""";
        _context.AuditEntries.Add(legacy);
        await _context.SaveChangesAsync();
        await _context.Database.ExecuteSqlRawAsync(AuditOperationBackfill.Sql);

        var item = await LatestAsync("Product", productId);
        var change = ChangeOf(item, "Product", productId);
        var measurement = FieldOf(change, "measurement");

        Assert.Equal("ProductUpdated", (string)item["kind"]!);
        Assert.Equal("Piece", (string)measurement["old"]!);
        Assert.Equal("Kilogram", (string)measurement["new"]!);
        Assert.False(HasField(change, "name"));
    }

    private static AuditEntry Legacy(string entityType, int entityId, DateTimeOffset at, int? userId, AuditAction action = AuditAction.Created) =>
        new()
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            NewValues = action == AuditAction.Created ? """{"Name":"Legacy"}""" : null,
            UserId = userId,
            TimestampUtc = at,
        };
}
