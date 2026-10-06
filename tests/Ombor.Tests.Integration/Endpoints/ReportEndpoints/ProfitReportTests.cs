using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Enums;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Payroll;
using Ombor.Contracts.Responses.Dashboard;
using Ombor.Contracts.Responses.Payment;
using Ombor.Contracts.Responses.Report;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ReportEndpoints;

/// <summary>Expenses, losses and the P&amp;L built from them — each checked against its own events and the others.</summary>
public sealed class ProfitReportTests(TestingWebApplicationFactory factory, ITestOutputHelper output)
    : ReportTestsBase(factory, output)
{
    [Fact]
    public async Task Profit_IsNetRevenueLessCostLossesPayrollAndOtherExpenses()
    {
        var before = await GetReportAsync<ProfitReportDto>("profit", TodayOnly);
        var dashboardBefore = await _client.GetAsync<DashboardDto>("dashboard?period=today");

        // Sale 3 @ 100 at cost 40 (+300 revenue, +120 cost), 1 unit written off (40), payroll 50, rent 20.
        var walletId = await CreateWalletAsync();
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10m, unitCost: 40m);
        await PostTransactionAsync(Document(TransactionType.Sale, partnerId, productId, warehouseId, 3m, 100m));
        await AdjustAsync(warehouseId, productId, "Decrease", 1m, "Damage");
        await PayPayrollAsync(await CreateEmployeeAsync(), walletId, 50m);
        await PayGeneralAsync(walletId, 20m, $"Rent {Guid.NewGuid():N}");

        var after = await GetReportAsync<ProfitReportDto>("profit", TodayOnly);
        var dashboardAfter = await _client.GetAsync<DashboardDto>("dashboard?period=today");

        Assert.Equal(300m, after.Totals.NetRevenue - before.Totals.NetRevenue);
        Assert.Equal(120m, after.Totals.Cost - before.Totals.Cost);
        Assert.Equal(180m, after.Totals.GrossProfit - before.Totals.GrossProfit);
        Assert.Equal(40m, after.Totals.Losses - before.Totals.Losses);
        Assert.Equal(50m, after.Totals.Payroll - before.Totals.Payroll);
        Assert.Equal(20m, after.Totals.OtherExpenses - before.Totals.OtherExpenses);
        Assert.Equal(70m, after.Totals.Profit - before.Totals.Profit);
        Assert.All(after.Rows, r => Assert.Equal(r.GrossProfit - r.Losses - r.Payroll - r.OtherExpenses, r.Profit));
        Assert.All(after.Rows, r => Assert.Equal(r.NetRevenue - r.Cost, r.GrossProfit));

        // The dashboard card is the same gross profit.
        Assert.Equal(180m, dashboardAfter.GrossProfit.Value - dashboardBefore.GrossProfit.Value);
        Assert.Equal(dashboardAfter.GrossProfit.Value, dashboardAfter.GrossProfit.Trend.Sum());
        Assert.Equal(after.Totals.GrossProfit, dashboardAfter.GrossProfit.Value);
    }

    [Fact]
    public async Task Profit_AgreesWithTheSalesLossesAndExpensesReports_ForTheSameDays()
    {
        var query = $"from={Day(Today.AddDays(-30))}&to={Day(Today)}";

        var profit = await GetReportAsync<ProfitReportDto>("profit", $"{query}&groupBy=Week");
        var sales = await GetReportAsync<SalesReportDto>("sales", query);
        var losses = await GetReportAsync<LossesReportDto>("losses", query);
        var expenses = await GetReportAsync<ExpensesReportDto>("expenses", query);

        Assert.Equal(sales.Totals.NetRevenue, profit.Totals.NetRevenue);
        Assert.Equal(sales.Totals.Cost, profit.Totals.Cost);
        Assert.Equal(sales.CostIsEstimated, profit.CostIsEstimated);
        Assert.Equal(losses.Value, profit.Totals.Losses);
        Assert.Equal(expenses.ByType.Single(t => t.Type == PaymentType.Payroll).Amount, profit.Totals.Payroll);
        Assert.Equal(expenses.ByType.Single(t => t.Type == PaymentType.General).Amount, profit.Totals.OtherExpenses);
        Assert.Equal(profit.Totals.Profit, profit.Rows.Sum(r => r.Profit));
    }

    [Fact]
    public async Task Expenses_GroupPayrollByEmployee_OtherByDescription_AndPartnersPaid_AndMatchTheCashFlow()
    {
        var walletId = await CreateWalletAsync();
        var employeeId = await CreateEmployeeAsync();
        var partnerId = await CreatePartnerAsync();
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        var rent = $"Rent {Guid.NewGuid():N}";
        await PayPayrollAsync(employeeId, walletId, 400m);
        await PayGeneralAsync(walletId, 70m, rent);
        await PayGeneralAsync(walletId, 30m, $"  {rent.ToUpperInvariant()} ");
        await PostTransactionAsync(Document(TransactionType.Supply, partnerId, productId, warehouseId, 4m, 50m, walletId: walletId, paid: 200m));

        var report = await GetReportAsync<ExpensesReportDto>("expenses", TodayOnly);
        var cashFlow = await GetReportAsync<CashFlowReportDto>("cash-flow", TodayOnly);

        var employee = Assert.Single(report.Employees, e => e.EmployeeId == employeeId);
        Assert.Equal((400m, 1), (employee.Amount, employee.Count));
        var other = Assert.Single(report.Other, o => string.Equals(o.Description, rent, StringComparison.OrdinalIgnoreCase));
        Assert.Equal((100m, 2), (other.Amount, other.Count));
        var partner = Assert.Single(report.Partners, p => p.PartnerId == partnerId);
        Assert.Equal((200m, 1), (partner.Amount, partner.Count));

        Assert.Equal(report.Total, report.ByType.Sum(t => t.Amount));
        Assert.Equal(cashFlow.Totals.Expense, report.Total);
    }

    [Fact]
    public async Task Losses_ValueDecreasesAtTheirSnapshottedCost_ByProductAndReason()
    {
        var warehouseId = await CreateWarehouseAsync();
        var productId = await CreateProductAsync();
        await AddOpeningStockAsync(warehouseId, productId, quantity: 10m, unitCost: 30m);
        var before = await GetReportAsync<LossesReportDto>("losses", TodayOnly);

        await AdjustAsync(warehouseId, productId, "Decrease", 2m, "Damage");
        await AdjustAsync(warehouseId, productId, "Decrease", 1m, "Theft");
        await AdjustAsync(warehouseId, productId, "Increase", 1m, "Found");

        var after = await GetReportAsync<LossesReportDto>("losses", TodayOnly);

        var product = Assert.Single(after.Products, p => p.ProductId == productId);
        Assert.Equal(3m, product.Quantity);
        Assert.Equal(90m, product.Value);
        Assert.Equal(2, product.Count);
        Assert.Equal(90m, after.Value - before.Value);
        Assert.Equal(60m, ReasonValue(after, "Damage") - ReasonValue(before, "Damage"));
        Assert.Equal(30m, ReasonValue(after, "Theft") - ReasonValue(before, "Theft"));
        Assert.DoesNotContain(after.Reasons, r => r.Reason == "Found");
    }

    [Theory]
    [InlineData("sales", "from=2026-10-02&to=2026-10-01", "From")]
    [InlineData("sales", "from=2020-01-01&to=2026-10-01", "To")]
    [InlineData("profit", "groupBy=Product", "GroupBy")]
    [InlineData("stock", "warehouseId=999999", "WarehouseId")]
    [InlineData("cash-flow", "walletId=999999", "WalletId")]
    [InlineData("cash-flow", "walletId=0", "WalletId")]
    public async Task Reports_RejectABadPeriodGroupingOrReference_With400OnTheField(string report, string query, string field)
    {
        var problem = await _client.GetAsync<ValidationProblemDetails>($"{Reports}/{report}?{query}", HttpStatusCode.BadRequest);

        Assert.Contains(field, problem.Errors.Keys);
    }

    private static decimal ReasonValue(LossesReportDto report, string reason) =>
        report.Reasons.SingleOrDefault(r => r.Reason == reason)?.Value ?? 0m;

    private Task<PaymentRecordDto> PayPayrollAsync(int employeeId, int walletId, decimal amount) =>
        _client.PostAsync<PaymentRecordDto>(
            $"employees/{employeeId}/payrolls",
            new CreatePayrollRequest(employeeId, walletId, amount, Period: "2026-10", Notes: null));

    private Task<PaymentRecordDto> PayGeneralAsync(int walletId, decimal amount, string description) =>
        _client.PostAsync<PaymentRecordDto>(
            "payments",
            new CreatePaymentRecordRequest(
                PaymentType.General, PaymentDirection.Expense, null, null, walletId, amount, description, null, []).ToMultipartFormData());
}
