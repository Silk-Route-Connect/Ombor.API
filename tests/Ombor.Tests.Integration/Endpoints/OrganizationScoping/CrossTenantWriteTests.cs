using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Order;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Payroll;
using Ombor.Contracts.Requests.StockAdjustment;
using Ombor.Contracts.Requests.Template;
using Ombor.Contracts.Requests.Transaction;
using Ombor.Contracts.Requests.Transfer;
using Ombor.Contracts.Requests.Wallet;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Persistence;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.OrganizationScoping;

/// <summary>
/// Writes must not accept another organization's ids (rule 34). The global filter only guards reads: before the
/// ownership check, an org-2 id satisfied the foreign key and the org-1 row pointed into org 2's ledger (e.g. an
/// order delivered as a Sale to a foreign customer changed that customer's balance). Every write resolves its ids
/// through the organization-filtered sets and answers 400 <c>validation.failed</c> on the offending field.
/// </summary>
public sealed class CrossTenantWriteTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : EndpointTestsBase(factory, output)
{
    private const int ForeignOrganizationId = 2;

    protected override string GetUrl() => Routes.Transaction;

    protected override string GetUrl(int id) => $"{Routes.Transaction}/{id}";

    [Fact]
    public async Task Sale_ShouldRejectForeignPartner_AndWriteNothing()
    {
        var foreign = await CreateForeignGraphAsync();
        var own = await CreateOwnGraphAsync();
        var before = await _context.Transactions.CountAsync();

        var problem = await PostTransactionAsync(Sale(foreign.PartnerId, own.ProductId, own.WarehouseId));

        AssertFieldError(problem, nameof(CreateTransactionRequest.PartnerId));
        Assert.Equal(before, await _context.Transactions.CountAsync());
    }

    [Fact]
    public async Task Supply_ShouldRejectForeignProductAndWarehouse()
    {
        var foreign = await CreateForeignGraphAsync();
        var own = await CreateOwnGraphAsync();

        var request = Sale(own.PartnerId, foreign.ProductId, foreign.WarehouseId) with { Type = TransactionType.Supply };
        var problem = await PostTransactionAsync(request);

        AssertFieldError(problem, "Lines[0].ProductId");
        AssertFieldError(problem, nameof(CreateTransactionRequest.WarehouseId));
    }

    [Fact]
    public async Task Sale_ShouldRejectForeignWalletAndSettlement()
    {
        var foreign = await CreateForeignGraphAsync();
        var own = await CreateOwnGraphAsync();

        var request = Sale(own.PartnerId, own.ProductId, own.WarehouseId) with
        {
            WalletId = foreign.WalletId,
            PaidAmount = 2_000m,
            Settlements = [new SettlementInput(foreign.TransactionId, 1_000m)],
        };
        var problem = await PostTransactionAsync(request);

        AssertFieldError(problem, nameof(CreateTransactionRequest.WalletId));
        AssertFieldError(problem, "Settlements[0].TransactionId");
    }

    [Fact]
    public async Task Order_ShouldRejectForeignCustomer_SoDeliveryCannotBookASaleIntoAnotherOrganization()
    {
        var foreign = await CreateForeignGraphAsync();
        var own = await CreateOwnGraphAsync();

        var request = new CreateOrderRequest(
            foreign.PartnerId, OrderSource.OmborWeb, own.WarehouseId, null, null, null, null,
            [new CreateOrderLineRequest(foreign.ProductId, 1m, 1_000m, null, DiscountType.Fixed)]);
        var problem = await _client.PostAsync<JObject>(Routes.Order, request, HttpStatusCode.BadRequest);

        AssertFieldError(problem, nameof(CreateOrderRequest.CustomerId));
        AssertFieldError(problem, "Lines[0].ProductId");
    }

    [Fact]
    public async Task Template_ShouldRejectForeignPartnerAndProduct()
    {
        var foreign = await CreateForeignGraphAsync();

        var request = new CreateTemplateRequest(
            foreign.PartnerId, $"Template {Guid.NewGuid():N}", TemplateType.Sale,
            [new CreateTemplateItem(foreign.ProductId, 1m, 1_000m, 0m)]);
        var problem = await _client.PostAsync<JObject>(Routes.Template, request, HttpStatusCode.BadRequest);

        AssertFieldError(problem, nameof(CreateTemplateRequest.PartnerId));
        AssertFieldError(problem, "Items[0].ProductId");
    }

    [Fact]
    public async Task Payment_ShouldRejectForeignWalletPartnerAndSettlement()
    {
        var foreign = await CreateForeignGraphAsync();

        var request = new CreatePaymentRecordRequest(
            PaymentType.Transaction, PaymentDirection.Income, foreign.PartnerId, null, foreign.WalletId,
            Amount: 1_000m, Description: null, Period: null,
            Settlements: [new SettlementInput(foreign.TransactionId, 1_000m)]);
        var problem = await _client.PostAsync<JObject>("payments", request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        AssertFieldError(problem, nameof(CreatePaymentRecordRequest.WalletId));
        AssertFieldError(problem, nameof(CreatePaymentRecordRequest.PartnerId));
        AssertFieldError(problem, "Settlements[0].TransactionId");
    }

    [Fact]
    public async Task Payroll_ShouldRejectForeignEmployee()
    {
        var foreign = await CreateForeignGraphAsync();
        var own = await CreateOwnGraphAsync();

        var request = new CreatePayrollRequest(foreign.EmployeeId, own.WalletId, 1_000m, "2026-10", null);
        var problem = await _client.PostAsync<JObject>(
            $"{Routes.Employee}/{foreign.EmployeeId}/payrolls", request, HttpStatusCode.BadRequest);

        AssertFieldError(problem, nameof(CreatePayrollRequest.EmployeeId));
    }

    [Fact]
    public async Task StockMoves_ShouldRejectForeignProducts()
    {
        var foreign = await CreateForeignGraphAsync();
        var own = await CreateOwnGraphAsync();
        var otherOwnWarehouse = await CreateOwnWarehouseAsync();

        var transfer = await _client.PostAsync<JObject>(
            Routes.Transfer,
            new CreateTransferRequest(own.WarehouseId, foreign.WarehouseId, null, [new CreateTransferLine(foreign.ProductId, 1m)]),
            HttpStatusCode.BadRequest);
        AssertFieldError(transfer, nameof(CreateTransferRequest.ToWarehouseId));
        AssertFieldError(transfer, "Lines[0].ProductId");

        var adjustment = await _client.PostAsync<JObject>(
            Routes.StockAdjustment,
            new CreateStockAdjustmentRequest(own.WarehouseId, foreign.ProductId, StockAdjustmentDirection.Increase, 1m, "Found", null),
            HttpStatusCode.BadRequest);
        AssertFieldError(adjustment, nameof(CreateStockAdjustmentRequest.ProductId));

        var opening = await _client.PostAsync<JObject>(
            $"{Routes.Warehouse}/{otherOwnWarehouse}/opening-stock",
            new AddOpeningStockRequest(otherOwnWarehouse, [new OpeningStockLine(foreign.ProductId, 5m, 10m)]),
            HttpStatusCode.BadRequest);
        AssertFieldError(opening, "Items[0].ProductId");

        // Nothing was created in the foreign organization's stock.
        await using var foreignContext = CreateContext(ForeignOrganizationId);
        Assert.False(await foreignContext.WarehouseItems.AnyAsync(i => i.WarehouseId == foreign.WarehouseId));
    }

    [Fact]
    public async Task WalletTransfer_ShouldRejectForeignWallet()
    {
        var foreign = await CreateForeignGraphAsync();
        var own = await CreateOwnGraphAsync();

        var problem = await _client.PostAsync<JObject>(
            $"{Routes.Wallet}/transfers",
            new CreateWalletTransferRequest(own.WalletId, foreign.WalletId, 10m, null),
            HttpStatusCode.BadRequest);

        AssertFieldError(problem, nameof(CreateWalletTransferRequest.ToWalletId));
    }

    private static void AssertFieldError(JObject problem, string field)
    {
        Assert.Equal("validation.failed", (string?)problem["code"]);
        Assert.NotNull(problem["errors"]?[field]);
    }

    private Task<JObject> PostTransactionAsync(CreateTransactionRequest request) =>
        _client.PostAsync<JObject>(Routes.Transaction, request.ToMultipartFormData(), HttpStatusCode.BadRequest);

    private static CreateTransactionRequest Sale(int partnerId, int productId, int warehouseId) => new(
        PartnerId: partnerId,
        Type: TransactionType.Sale,
        Notes: null,
        Lines: [new CreateTransactionLine(productId, UnitPrice: 1_000m, Discount: 0m, DiscountType.Fixed, Quantity: 1m)],
        WalletId: null,
        PaidAmount: 0m,
        Settlements: null,
        Overpayment: OverpaymentHandling.Change,
        Attachments: [],
        WarehouseId: warehouseId);

    private async Task<TenantGraph> CreateOwnGraphAsync()
    {
        await using var ownContext = CreateContext(1);
        return await TenantGraph.CreateAsync(ownContext, withStock: true);
    }

    private async Task<TenantGraph> CreateForeignGraphAsync()
    {
        await using var foreignContext = CreateContext(ForeignOrganizationId);
        return await TenantGraph.CreateAsync(foreignContext, withStock: false);
    }

    private async Task<int> CreateOwnWarehouseAsync()
    {
        await using var ownContext = CreateContext(1);
        var warehouse = new Warehouse { Name = $"Warehouse {Guid.NewGuid():N}", Location = "Tashkent" };
        ownContext.Warehouses.Add(warehouse);
        await ownContext.SaveChangesAsync();

        return warehouse.Id;
    }

    private ApplicationDbContext CreateContext(int organizationId)
    {
        var options = _factory.Services.GetRequiredService<DbContextOptions<ApplicationDbContext>>();

        return new ApplicationDbContext(options, new FakeOrganizationAccessor(organizationId));
    }
}
