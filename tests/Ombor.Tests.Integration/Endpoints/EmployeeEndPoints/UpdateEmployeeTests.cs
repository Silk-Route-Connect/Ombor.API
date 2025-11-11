using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.EmployeeEndPoints;

public class UpdateEmployeeTests(
    TestingWebApplicationFactory factory, ITestOutputHelper outputHelper) : EmployeeTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PutAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var employeeId = await CreateEmployeeAsync();
        var request = EmployeeRequestFactory.GenerateValidUpdateRequest(employeeId);
        var url = GetUrl(employeeId);

        // Act
        var response = await _client.PutAsync<UpdateEmployeeResponse>(url, request);

        // Assert
        await _responseValidator.Employee.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var employeeId = await CreateEmployeeAsync();
        var request = EmployeeRequestFactory.GenerateInvalidUpdateRequest(employeeId);
        var url = GetUrl(employeeId);

        // Act
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(request.Name), response.Errors.Keys);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenEmployeeDoesNotExist()
    {
        // Arrange
        var request = EmployeeRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act
        var response = await _client.PutAsync<ProblemDetails>(NotFoundUrl, request, HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Employee>(NonExistentEntityId);
    }
}
