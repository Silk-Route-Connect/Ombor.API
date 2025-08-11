using System.Net;
using Microsoft.AspNetCore.Mvc;
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
    public async Task DeleteAsync_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Employee>(NonExistentEntityId);
    }
}
