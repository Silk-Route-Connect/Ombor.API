using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Employee;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.EmployeeEndPoints;

public class GetEmployeeByIdTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : EmployeeTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange
        var url = NotFoundUrl;

        // Act
        var response = await _client.GetAsync<ProblemDetails>(url, HttpStatusCode.NotFound);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(NotFoundTitle, response.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEmployee_WhenEmployeeExists()
    {
        // Arrange
        var employeeId = await CreateEmployeeAsync();
        var url = GetUrl(employeeId);

        // Act
        var response = await _client.GetAsync<EmployeeDto>(url);

        // Assert
        await _responseValidator.Employee.ValidateGetByIdAsync(employeeId, response);
    }

}
