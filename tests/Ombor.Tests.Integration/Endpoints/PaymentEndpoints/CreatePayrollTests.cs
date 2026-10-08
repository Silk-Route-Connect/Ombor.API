using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Requests.Payment;
using Ombor.Contracts.Requests.Payroll;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Persistence.DataFixes;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.PaymentEndpoints;

public sealed class CreatePayrollTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : PaymentTestsBase(factory, outputHelper)
{
    private static string PayrollUrl(int employeeId) => $"employees/{employeeId}/payrolls";

    [Fact]
    public async Task PostAsync_ShouldRecordPayroll_AsWalletSourcedExpense()
    {
        // Arrange
        var walletId = await CreateWalletAsync(10_000_000m); // funded: payroll may not overdraw the wallet (DR-25)
        var employeeId = await CreateEmployeeAsync(salary: 5_000_000m);

        var request = new CreatePayrollRequest(employeeId, walletId, Amount: 4_000_000m, Period: "2026-06", Notes: "June salary");

        // Act
        var payroll = await _client.PostAsync<PaymentRecordDto>(PayrollUrl(employeeId), request);

        // Assert
        Assert.Equal("Payroll", payroll.Type);
        Assert.Equal("Expense", payroll.Direction);
        Assert.Equal(employeeId, payroll.EmployeeId);
        Assert.Equal("2026-06", payroll.Period);
        Assert.Equal(5_000_000m, payroll.Salary); // snapshot of the employee's salary
        Assert.True(int.TryParse(payroll.Number, out _)); // bare sequential number, no "P-" prefix

        var source = Assert.Single(payroll.Sources);
        Assert.Equal("Wallet", source.SourceType);
        Assert.Equal(walletId, source.WalletId);
        Assert.Equal(4_000_000m, source.Amount); // the amount paid, not the salary
        Assert.Empty(payroll.Allocations);
    }

    [Fact]
    public async Task PostAsync_ShouldSnapshotSalary_IndependentOfLaterSalaryChange()
    {
        // Arrange
        var walletId = await CreateWalletAsync(10_000_000m); // funded: payroll may not overdraw the wallet (DR-25)
        var employeeId = await CreateEmployeeAsync(salary: 5_000_000m);

        var request = new CreatePayrollRequest(employeeId, walletId, Amount: 5_000_000m, Period: "2026-06", Notes: null);
        var payroll = await _client.PostAsync<PaymentRecordDto>(PayrollUrl(employeeId), request);

        // Act — the employee gets a raise after the payroll was paid.
        var employee = await _context.Employees.FirstAsync(e => e.Id == employeeId);
        employee.Salary = 7_000_000m;
        await _context.SaveChangesAsync();

        // Assert — the recorded payroll still shows the salary at the time it was paid.
        var reread = await _client.GetAsync<PaymentRecordDto>($"payments/{payroll.Id}");
        Assert.Equal(5_000_000m, reread.Salary);
    }

    [Fact]
    public async Task PostAsync_ShouldAllowMultiplePayrolls_ForSameEmployeeAndPeriod()
    {
        // Arrange
        var walletId = await CreateWalletAsync(10_000_000m); // funded: payroll may not overdraw the wallet (DR-25)
        var employeeId = await CreateEmployeeAsync(salary: 5_000_000m);

        var first = new CreatePayrollRequest(employeeId, walletId, Amount: 2_000_000m, Period: "2026-06", Notes: "advance");
        var second = new CreatePayrollRequest(employeeId, walletId, Amount: 3_000_000m, Period: "2026-06", Notes: "remainder");

        // Act
        await _client.PostAsync<PaymentRecordDto>(PayrollUrl(employeeId), first);
        await _client.PostAsync<PaymentRecordDto>(PayrollUrl(employeeId), second);

        // Assert — both are recorded; no one-per-month constraint.
        var payrolls = await _client.GetAsync<PaymentRecordDto[]>(PayrollUrl(employeeId));
        var forPeriod = payrolls.Where(p => p.Period == "2026-06").ToArray();
        Assert.Equal(2, forPeriod.Length);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequestOnWalletId_WhenWalletMissing()
    {
        var employeeId = await CreateEmployeeAsync(salary: 5_000_000m);

        var request = new CreatePayrollRequest(employeeId, NonExistentEntityId, Amount: 1_000m, Period: "2026-06", Notes: null);

        var problem = await _client.PostAsync<ValidationProblemDetails>(PayrollUrl(employeeId), request, HttpStatusCode.BadRequest);
        Assert.Contains(nameof(CreatePayrollRequest.WalletId), problem.Errors.Keys);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenAmountNotPositive()
    {
        var walletId = await CreateWalletAsync(10_000_000m); // funded: payroll may not overdraw the wallet (DR-25)
        var employeeId = await CreateEmployeeAsync(salary: 5_000_000m);

        var request = new CreatePayrollRequest(employeeId, walletId, Amount: 0m, Period: "2026-06", Notes: null);

        await _client.PostAsync<ValidationProblemDetails>(PayrollUrl(employeeId), request, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("Июнь 2026")]
    [InlineData("2026-6")]
    [InlineData("2026-13")]
    [InlineData("06-2026")]
    public async Task PostAsync_ShouldReturnBadRequestOnPeriod_WhenNotYearMonth(string period)
    {
        var walletId = await CreateWalletAsync(10_000_000m);
        var employeeId = await CreateEmployeeAsync(salary: 5_000_000m);

        var request = new CreatePayrollRequest(employeeId, walletId, Amount: 1_000m, Period: period, Notes: null);

        var problem = await _client.PostAsync<ValidationProblemDetails>(PayrollUrl(employeeId), request, HttpStatusCode.BadRequest);
        Assert.Contains(nameof(CreatePayrollRequest.Period), problem.Errors.Keys);
    }

    [Fact]
    public async Task PaymentsModulePayroll_ShouldRecordPeriodAndSalary_LikeTheEmployeePage()
    {
        var walletId = await CreateWalletAsync(10_000_000m);
        var employeeId = await CreateEmployeeAsync(salary: 3_000_000m);
        var request = new CreatePaymentRecordRequest(
            Contracts.Enums.PaymentType.Payroll, Contracts.Enums.PaymentDirection.Expense, null, employeeId, walletId,
            Amount: 1_500_000m, Description: null, Period: "2026-10", Settlements: []);

        var payment = await _client.PostAsync<PaymentRecordDto>(GetUrl(), request.ToMultipartFormData());

        Assert.Equal("2026-10", payment.Period);
        Assert.Equal(3_000_000m, payment.Salary);
    }

    [Fact]
    public async Task PaymentsModulePayroll_ShouldRejectALabelPeriod()
    {
        var walletId = await CreateWalletAsync(10_000_000m);
        var employeeId = await CreateEmployeeAsync(salary: 3_000_000m);
        var request = new CreatePaymentRecordRequest(
            Contracts.Enums.PaymentType.Payroll, Contracts.Enums.PaymentDirection.Expense, null, employeeId, walletId,
            Amount: 1_000m, Description: null, Period: "Iyun 2026", Settlements: []);

        var problem = await _client.PostAsync<ValidationProblemDetails>(GetUrl(), request.ToMultipartFormData(), HttpStatusCode.BadRequest);

        Assert.Contains(nameof(CreatePaymentRecordRequest.Period), problem.Errors.Keys);
    }

    [Fact]
    public async Task Backfill_ShouldConvertLabelPeriods_ToYearMonth_AndLeaveTheRest()
    {
        // Arrange — periods as the payments module once saved them, in each language, plus values to keep.
        var walletId = await CreateWalletAsync(0m);
        var employeeId = await CreateEmployeeAsync(salary: 1_000m);
        var ids = new Dictionary<string, int>();
        foreach (var period in new[] { "Июнь 2026", "Iyun 2026", "сентября 2025", "Декабр 2024", "2026-07", "unreadable" })
        {
            ids[period] = await PlantPayrollAsync(employeeId, walletId, period);
        }

        // Act
        await _context.Database.ExecuteSqlRawAsync(PayrollPeriodBackfill.Sql);

        // Assert
        async Task<string?> PeriodOf(string original) =>
            (await _context.Payments.AsNoTracking().FirstAsync(p => p.Id == ids[original])).Period;

        Assert.Equal("2026-06", await PeriodOf("Июнь 2026"));
        Assert.Equal("2026-06", await PeriodOf("Iyun 2026"));
        Assert.Equal("2025-09", await PeriodOf("сентября 2025"));
        Assert.Equal("2024-12", await PeriodOf("Декабр 2024"));
        Assert.Equal("2026-07", await PeriodOf("2026-07"));
        Assert.Equal("unreadable", await PeriodOf("unreadable"));
        Assert.Equal(0, await _context.Database.ExecuteSqlRawAsync(PayrollPeriodBackfill.Sql)); // idempotent
    }

    private async Task<int> PlantPayrollAsync(int employeeId, int walletId, string period)
    {
        var payment = new Payment
        {
            Type = PaymentType.Payroll,
            Direction = PaymentDirection.Expense,
            DateUtc = DateTimeOffset.UtcNow,
            EmployeeId = employeeId,
            WalletId = walletId,
            Period = period,
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return payment.Id;
    }

    private async Task<int> CreateEmployeeAsync(decimal salary)
    {
        var employee = new Employee
        {
            FullName = $"Employee {Guid.NewGuid():N}",
            Position = "Cashier",
            Salary = salary,
            Status = EmployeeStatus.Active,
            DateOfEmployment = new DateOnly(2026, 1, 1),
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return employee.Id;
    }
}
