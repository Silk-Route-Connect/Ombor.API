using System.Net;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Ombor.Contracts.Requests.Payroll;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.EmployeeEndPoints;

public class DeleteEmployeeTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : EmployeeTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenEmployeeExists()
    {
        // Arrange
        var employeeToDelete = _builder.EmployeeBuilder
            .WithFullName("Employee to delete")
            .Build();

        var employeeId = await CreateEmployeeAsync(employeeToDelete);
        var url = GetUrl(employeeId);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Employee.ValidateDeleteAsync(employeeId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnConflict_WhenPayrollReferencesTheEmployee()
    {
        // Arrange — an employee who was paid once: payroll is an immutable money event that names them.
        var employeeId = await CreateEmployeeAsync(_builder.EmployeeBuilder.WithFullName("Paid employee").Build());
        var wallet = new Wallet
        {
            Name = $"Wallet {Guid.NewGuid():N}",
            Type = Ombor.Domain.Enums.WalletType.Cash,
            OpeningBalance = 10_000m,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();
        await _client.PostAsync<JObject>(
            $"{GetUrl(employeeId)}/payrolls",
            new CreatePayrollRequest(employeeId, wallet.Id, 1_000m, "2026-10", null));

        // Act
        var before = await _client.GetAsync<JObject>(GetUrl(employeeId));
        var problem = await _client.DeleteAsync<JObject>(GetUrl(employeeId), HttpStatusCode.Conflict);

        // Assert — 409 entity.referenced (not the old FK 500), the flag agrees, and the employee is still there.
        Assert.False((bool?)before["isDeletable"]);
        Assert.Equal("entity.referenced", (string?)problem["code"]);
        await _client.GetAsync<JObject>(GetUrl(employeeId));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldServeDeletable_WhenNoPaymentReferencesTheEmployee()
    {
        var employeeId = await CreateEmployeeAsync(_builder.EmployeeBuilder.WithFullName("Unpaid employee").Build());

        var employee = await _client.GetAsync<JObject>(GetUrl(employeeId));

        Assert.True((bool?)employee["isDeletable"]);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Employee>(NonExistentEntityId);
    }
}
