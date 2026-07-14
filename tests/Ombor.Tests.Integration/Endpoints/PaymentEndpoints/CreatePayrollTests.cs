using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ombor.Contracts.Requests.Payroll;
using Ombor.Contracts.Responses.Payment;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
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
        Assert.StartsWith("P-", payroll.Number);

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
    public async Task PostAsync_ShouldReturnNotFound_WhenWalletMissing()
    {
        var employeeId = await CreateEmployeeAsync(salary: 5_000_000m);

        var request = new CreatePayrollRequest(employeeId, NonExistentEntityId, Amount: 1_000m, Period: "2026-06", Notes: null);

        await _client.PostAsync<ProblemDetails>(PayrollUrl(employeeId), request, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnBadRequest_WhenAmountNotPositive()
    {
        var walletId = await CreateWalletAsync(10_000_000m); // funded: payroll may not overdraw the wallet (DR-25)
        var employeeId = await CreateEmployeeAsync(salary: 5_000_000m);

        var request = new CreatePayrollRequest(employeeId, walletId, Amount: 0m, Period: "2026-06", Notes: null);

        await _client.PostAsync<ValidationProblemDetails>(PayrollUrl(employeeId), request, HttpStatusCode.BadRequest);
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
