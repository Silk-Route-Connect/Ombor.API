using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Employee;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.EmployeeEndPoints;

public class CreateEmployeeTests(
    TestingWebApplicationFactory factory,
    ITestOutputHelper outputHelper) : EmployeeTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task CreateAsync_ShouldRetuenCreated_WhenEmployeeIsValid()
    {
        // Arrange
        var request = EmployeeRequestFactory.GenerateValidCreateRequest();

        // Act
        var response = await _client.PostAsync<CreateEmployeeResponse>(Routes.Employee, request, HttpStatusCode.Created);

        // Assert
        await _responseValidator.Employee.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnBadRequest_WhenEmployeeIsInvalid()
    {
        // Arrange
        var request = EmployeeRequestFactory.GenerateInvalidCreateRequest();

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(Routes.Employee, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Employee.FullName), response.Errors.Keys);
    }
}
