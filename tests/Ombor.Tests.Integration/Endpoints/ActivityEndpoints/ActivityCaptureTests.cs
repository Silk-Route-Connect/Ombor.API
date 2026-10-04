using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Employee;
using Ombor.Contracts.Responses.Transaction;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;
using ContractTemplateType = Ombor.Contracts.Enums.TemplateType;
using DomainTemplateType = Ombor.Domain.Enums.TemplateType;

namespace Ombor.Tests.Integration.Endpoints.ActivityEndpoints;

/// <summary>
/// mvp §17 done-criterion: a master-data edit and a money event both appear with actor and before/after values —
/// plus archive/restore as their own actions, parts of a record (packaging, contacts, template items) inside its
/// history, and one operation per request.
/// </summary>
public sealed class ActivityCaptureTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ActivityTestsBase(factory, output)
{
    [Fact]
    public async Task ProductEdit_AppearsWithActor_AndBeforeAfterValues()
    {
        var productId = await CreateProductAsync();
        var product = await _context.Products.AsNoTracking().SingleAsync(p => p.Id == productId);
        var request = ProductRequestFactory.GenerateValidUpdateRequest(productId, product.CategoryId);

        await _client.PutAsync<JObject>($"products/{productId}", request.ToMultipartFormData());

        var item = await LatestAsync("Product", productId);
        Assert.Equal("ProductUpdated", (string)item["kind"]!);
        Assert.Equal(CurrentUserId, (int)item["actor"]!["id"]!);
        Assert.False(string.IsNullOrWhiteSpace((string)item["actor"]!["name"]!));
        Assert.Equal("Product", (string)item["primary"]!["entityKind"]!);
        Assert.Equal(request.Name, (string)item["primary"]!["label"]!);
        Assert.Null(item["amount"]);

        var change = ChangeOf(item, "Product", productId);
        Assert.Equal("Updated", (string)change["action"]!);
        Assert.Equal(product.Name, (string)FieldOf(change, "name")["old"]!);
        Assert.Equal(request.Name, (string)FieldOf(change, "name")["new"]!);
        Assert.Equal(50m, (decimal)FieldOf(change, "supplyPrice")["old"]!);
        Assert.Equal(90m, (decimal)FieldOf(change, "supplyPrice")["new"]!);
        Assert.Equal("Piece", (string)FieldOf(change, "measurement")["old"]!);
        Assert.Equal("Kilogram", (string)FieldOf(change, "measurement")["new"]!);
        Assert.True(HasField(change, "packaging.size"));
        Assert.False(HasField(change, "salePrice"));
        Assert.False(HasField(change, "id"));
        Assert.False(HasField(change, "organizationId"));
    }

    [Fact]
    public async Task ArchiveAndRestore_AreTheirOwnActions()
    {
        var productId = await CreateProductAsync();

        await _client.PostAsync($"products/{productId}/archive");
        var archived = await LatestAsync("Product", productId);

        await _client.PostAsync($"products/{productId}/restore");
        var restored = await LatestAsync("Product", productId);

        Assert.Equal("ProductArchived", (string)archived["kind"]!);
        var archive = ChangeOf(archived, "Product", productId);
        Assert.Equal("Archived", (string)archive["action"]!);
        Assert.False((bool)FieldOf(archive, "isArchived")["old"]!);
        Assert.True((bool)FieldOf(archive, "isArchived")["new"]!);

        Assert.Equal("ProductRestored", (string)restored["kind"]!);
        Assert.Equal("Restored", (string)ChangeOf(restored, "Product", productId)["action"]!);

        var archivedOnly = await GetActivityAsync("entityKind=Product&action=Archived&pageSize=100");
        Assert.Contains(Items(archivedOnly), i => (string)i["operationId"]! == (string)archived["operationId"]!);
        Assert.DoesNotContain(Items(archivedOnly), i => (string)i["operationId"]! == (string)restored["operationId"]!);
    }

    [Fact]
    public async Task Sale_IsOneOperation_WithActorAmountLinesStockAndPayment()
    {
        var partnerId = await CreatePartnerAsync();
        var walletId = await CreateWalletAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await SeedStockAsync(warehouseId, productId, quantity: 100);
        var productName = await _context.Products.Where(p => p.Id == productId).Select(p => p.Name).SingleAsync();
        var request = TransactionRequestFactory.Sale(partnerId, productId, warehouseId, due: 10_000m, walletId, paidAmount: 10_000m);

        var sale = await _client.PostAsync<TransactionDto>("transactions", request.ToMultipartFormData());

        var item = await LatestAsync("Sale", sale.Id);
        Assert.Equal("SaleCreated", (string)item["kind"]!);
        Assert.Equal(CurrentUserId, (int)item["actor"]!["id"]!);
        Assert.Equal(10_000m, (decimal)item["amount"]!);
        Assert.Equal(sale.Id, (int)item["primary"]!["entityId"]!);
        Assert.Equal(sale.Number, (string)item["primary"]!["label"]!);

        var changes = (JArray)item["changes"]!;
        Assert.Equal(changes.Count, (int)item["changeCount"]!);

        // Saved, then settled within the request: one net creation with the final values.
        var document = (JObject)Assert.Single(changes, c => (string)c["entityKind"]! == "Sale");
        Assert.Same(changes[0], document);
        Assert.Equal("Created", (string)document["action"]!);
        Assert.Equal(10_000m, (decimal)FieldOf(document, "totalPaid")["new"]!);
        Assert.Equal("Closed", (string)FieldOf(document, "status")["new"]!);
        Assert.Equal(sale.Number, (string)FieldOf(document, "number")["new"]!);

        var line = ChangeOf(item, "TransactionLine");
        Assert.Equal("Created", (string)line["action"]!);
        Assert.Equal(productName, (string)line["label"]!);
        Assert.Equal(productName, (string)FieldOf(line, "product")["new"]!);
        Assert.Equal(1m, (decimal)FieldOf(line, "quantity")["new"]!);
        Assert.False(HasField(line, "transaction"));

        var stock = ChangeOf(item, "Stock");
        Assert.Equal("Updated", (string)stock["action"]!);
        Assert.Equal(100m, (decimal)FieldOf(stock, "quantity")["old"]!);
        Assert.Equal(99m, (decimal)FieldOf(stock, "quantity")["new"]!);

        Assert.Equal("Created", (string)ChangeOf(item, "Payment")["action"]!);
        Assert.Contains(changes, c => (string)c["entityKind"]! == "PaymentAllocation" && (string?)c["label"] == sale.Number);

        var sales = await GetActivityAsync("entityKind=Sale&pageSize=100");
        var supplies = await GetActivityAsync("entityKind=Supply&pageSize=100");
        Assert.Contains(Items(sales), i => (string)i["operationId"]! == (string)item["operationId"]!);
        Assert.DoesNotContain(Items(supplies), i => (string)i["operationId"]! == (string)item["operationId"]!);

        var detail = await _client.GetAsync<JObject>($"{ActivityRoute}/{item["operationId"]}");
        Assert.Equal("SaleCreated", (string)detail["kind"]!);
        Assert.Equal(changes.Count, ((JArray)detail["changes"]!).Count);
    }

    [Fact]
    public async Task EmployeeContacts_AreRecordedAsPartOfTheEmployee()
    {
        var create = EmployeeRequestFactory.GenerateValidCreateRequest();
        var employee = await _client.PostAsync<CreateEmployeeResponse>("employees", create);

        var created = await LatestAsync("Employee", employee.Id);
        Assert.Equal("EmployeeCreated", (string)created["kind"]!);
        Assert.Equal(create.ContactInfo!.Email, (string)FieldOf(ChangeOf(created, "Employee"), "contactInfo.email")["new"]!);

        var update = EmployeeRequestFactory.GenerateValidUpdateRequest(employee.Id) with
        {
            ContactInfo = create.ContactInfo with { Email = "changed@mail.com" },
        };
        await _client.PutAsync<UpdateEmployeeResponse>($"employees/{employee.Id}", update);

        var updated = await LatestAsync("Employee", employee.Id);
        var change = ChangeOf(updated, "Employee", employee.Id);
        Assert.Equal("EmployeeUpdated", (string)updated["kind"]!);
        Assert.Equal(create.ContactInfo.Email, (string)FieldOf(change, "contactInfo.email")["old"]!);
        Assert.Equal("changed@mail.com", (string)FieldOf(change, "contactInfo.email")["new"]!);
        Assert.Equal(1_000m, (decimal)FieldOf(change, "salary")["old"]!);
        Assert.Equal(50_000m, (decimal)FieldOf(change, "salary")["new"]!);
        Assert.False(HasField(change, "contactInfo.phoneNumbers"));
    }

    [Fact]
    public async Task TemplateItemEdit_ShowsInTheTemplatesHistory()
    {
        var partnerId = await CreatePartnerAsync();
        var productId = await CreateProductAsync();
        var template = new Template
        {
            Name = $"Template {Guid.NewGuid():N}",
            Type = DomainTemplateType.Sale,
            PartnerId = partnerId,
            Partner = null!,
            Items = [new TemplateItem { ProductId = productId, Quantity = 2, UnitPrice = 1_000m, Product = null!, Template = null! }],
        };
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        var itemId = template.Items[0].Id;

        var update = new UpdateTemplateRequest(
            template.Id, partnerId, template.Name, ContractTemplateType.Sale, [new UpdateTemplateItem(itemId, productId, 5, 1_000m, 0)]);
        await _client.PutAsync<JObject>($"templates/{template.Id}", update);

        var item = await LatestAsync("Template", template.Id);
        Assert.Equal("TemplateUpdated", (string)item["kind"]!);
        Assert.Equal("Template", (string)item["primary"]!["entityKind"]!);
        Assert.Equal(template.Id, (int)item["primary"]!["entityId"]!);
        Assert.Equal(template.Name, (string)item["primary"]!["label"]!);

        var line = ChangeOf(item, "TemplateItem", itemId);
        Assert.Equal("Updated", (string)line["action"]!);
        Assert.Equal(2m, (decimal)FieldOf(line, "quantity")["old"]!);
        Assert.Equal(5m, (decimal)FieldOf(line, "quantity")["new"]!);
        Assert.False(HasField(line, "template"));
    }

    [Fact]
    public async Task OrganizationProfileEdit_IsAuditedUnderTheOrganization()
    {
        var newName = $"Org {Guid.NewGuid():N}";
        var form = new MultipartFormDataContent { { new StringContent(newName), "Name" } };

        await _client.PutAsync<JObject>("settings/organization", form);

        var page = await GetActivityAsync("entityKind=Organization&entityId=1");
        var item = (JObject)Items(page)[0];
        Assert.Equal("OrganizationUpdated", (string)item["kind"]!);
        Assert.Equal(newName, (string)FieldOf(ChangeOf(item, "Organization", 1), "name")["new"]!);
    }

    [Fact]
    public async Task LargeOperation_ListItemIsCapped_DetailHasEveryChange()
    {
        var warehouseId = await CreateWarehouseAsync();
        var productIds = new List<int>();
        for (var i = 0; i < 30; i++)
        {
            productIds.Add(await CreateProductAsync());
        }

        var request = new AddOpeningStockRequest(warehouseId, [.. productIds.Select(id => new OpeningStockLine(id, 10m, 100m))]);
        await _client.PostAsync<JObject>($"warehouses/{warehouseId}/opening-stock", request, HttpStatusCode.OK);

        var page = await GetActivityAsync("entityKind=OpeningStock&pageSize=1");
        var item = (JObject)Items(page)[0];
        Assert.Equal("OpeningStockCreated", (string)item["kind"]!);
        Assert.Equal(60, (int)item["changeCount"]!);
        Assert.Equal(50, ((JArray)item["changes"]!).Count);
        Assert.Equal(30, item["changes"]!.Count(c => (string)c["entityKind"]! == "OpeningStock"));

        var detail = await _client.GetAsync<JObject>($"{ActivityRoute}/{item["operationId"]}");
        Assert.Equal(60, ((JArray)detail["changes"]!).Count);
    }
}
