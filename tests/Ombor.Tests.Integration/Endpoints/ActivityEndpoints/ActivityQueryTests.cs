using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ActivityEndpoints;

/// <summary>Paging and filters of <c>GET /api/activity</c>, and organization isolation (rule 34).</summary>
public sealed class ActivityQueryTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ActivityTestsBase(factory, output)
{
    [Fact]
    public async Task Paging_IsNewestFirst_WithTheTotal()
    {
        var first = await CreateCategoryAsync();
        var second = await CreateCategoryAsync();
        var third = await CreateCategoryAsync();

        var page1 = await GetActivityAsync("entityKind=Category&action=Created&pageSize=2&page=1");
        var page2 = await GetActivityAsync("entityKind=Category&action=Created&pageSize=2&page=2");

        Assert.True((int)page1["total"]! >= 3);
        Assert.Equal((int)page1["total"]!, (int)page2["total"]!);
        Assert.Equal([third, second], Items(page1).Select(i => (int)i["primary"]!["entityId"]!));
        Assert.Equal(first, (int)Items(page2)[0]["primary"]!["entityId"]!);
        Assert.True((DateTime)Items(page1)[0]["at"]! >= (DateTime)Items(page1)[1]["at"]!);
    }

    [Fact]
    public async Task UserFilter_KeepsOnlyThatActorsOperations()
    {
        var categoryId = await CreateCategoryAsync();

        var mine = await GetActivityAsync($"entityKind=Category&entityId={categoryId}&userId={CurrentUserId}");
        var someoneElse = await GetActivityAsync($"entityKind=Category&entityId={categoryId}&userId={NonExistentEntityId}");

        Assert.Single(Items(mine));
        Assert.Equal(0, (int)someoneElse["total"]!);
    }

    [Fact]
    public async Task DateFilter_UsesTheLocalCalendarDay()
    {
        var categoryId = await CreateCategoryAsync();
        var today = BusinessDay.Today.ToString("yyyy-MM-dd");
        var tomorrow = BusinessDay.Today.AddDays(1).ToString("yyyy-MM-dd");

        var todays = await GetActivityAsync($"entityKind=Category&entityId={categoryId}&from={today}&to={today}");
        var future = await GetActivityAsync($"from={tomorrow}");

        Assert.Single(Items(todays));
        Assert.Equal(0, (int)future["total"]!);
    }

    [Fact]
    public async Task ActionFilter_MatchesTheRecordsThemselves_NotTheirLinesOrSettlements()
    {
        var categoryId = await CreateCategoryAsync();
        var name = await _context.Categories.Where(c => c.Id == categoryId).Select(c => c.Name).SingleAsync();
        await _client.DeleteAsync($"categories/{categoryId}");

        var deleted = await GetActivityAsync("entityKind=Category&action=Deleted&pageSize=100");
        var deletion = Assert.Single(Items(deleted), i => (int)i["primary"]!["entityId"]! == categoryId);
        Assert.Equal("CategoryDeleted", (string)deletion["kind"]!);
        Assert.Equal(name, (string)deletion["primary"]!["label"]!);
        Assert.Equal(name, (string)FieldOf(ChangeOf((JObject)deletion, "Category"), "name")["old"]!);

        var sale = await CreateSaleAsync();
        var updated = await GetActivityAsync("action=Updated&pageSize=100");
        Assert.DoesNotContain(Items(updated), i => (string)i["kind"]! == "SaleCreated" && (int)i["primary"]!["entityId"]! == sale.Id);
    }

    [Fact]
    public async Task OtherOrganizations_ActivityIsInvisible()
    {
        int foreignCategoryId;
        await using (var otherOrganization = CreateContext(organizationId: 2))
        {
            var category = new Category { Name = $"Foreign {Guid.NewGuid():N}" };
            otherOrganization.Categories.Add(category);
            await otherOrganization.SaveChangesAsync();
            foreignCategoryId = category.Id;
        }

        var row = await _context.AuditEntries.IgnoreQueryFilters().AsNoTracking()
            .SingleAsync(a => a.EntityType == nameof(Category) && a.EntityId == foreignCategoryId);
        Assert.Equal(2, row.OrganizationId);
        Assert.NotNull(row.OperationId);

        var all = await GetActivityAsync("entityKind=Category&pageSize=100");
        var history = await GetActivityAsync($"entityKind=Category&entityId={foreignCategoryId}");

        Assert.DoesNotContain(Items(all), i => (int)i["primary"]!["entityId"]! == foreignCategoryId);
        Assert.Equal(0, (int)history["total"]!);
        await _client.GetAsync($"{ActivityRoute}/{row.OperationId}", HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=0")]
    [InlineData("page=0")]
    [InlineData("entityId=5")]
    [InlineData("from=2026-10-05&to=2026-10-04")]
    public Task InvalidQuery_Returns400(string query) =>
        _client.GetAsync($"{ActivityRoute}?{query}", HttpStatusCode.BadRequest);

    [Fact]
    public Task UnknownOperation_Returns404() =>
        _client.GetAsync($"{ActivityRoute}/{Guid.NewGuid()}", HttpStatusCode.NotFound);

    private async Task<int> CreateCategoryAsync()
    {
        var request = new CreateCategoryRequest($"Category {Guid.NewGuid():N}", "Activity test");
        var response = await _client.PostAsync<CreateCategoryResponse>("categories", request);

        return response.Id;
    }

    private async Task<TransactionDto> CreateSaleAsync()
    {
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 10);
        var request = TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 1_000m, walletId, paidAmount: 1_000m);

        return await _client.PostAsync<TransactionDto>("transactions", request.ToMultipartFormData());
    }
}
