using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Supplier;
using Ombor.Contracts.Responses.Supplier;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Endpoints.ProductEndpoints;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.SupplierEndpoints;

public class UpdateSupplierTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
        : SupplierTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task PutAsync_ShouldReturnOk_WhenRequestIsValid()
    {
        // Arrange 
        var supplier = _builder.SupplierBuilder
        .WithName("Supplier to update")
        .WithAddress("Supplier's address to update")
        .WithEmail("Supplier's email to update")
        .WithCompanyName("Supplier's company name")
        .WithIsActive(true)
        .WithPhoneNumbers(["+998914564561"])
        .Build();

        var supplierId = await CreateSupplierAsync(supplier);
        var request = CreateValidRequest(supplierId);
        var url = GetUrl(supplierId);

        // Act
        var response = await _client.PutAsync<UpdateSupplierResponse>(url, request);

        // Assert
        await _responseValidator.Supplier.ValidatePutAsync(request, response);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnNotFound_WhenSupplierDoesNotExist()
    {
        // Arrange
        var request = CreateValidRequest(NonExistentEntityId);

        // Act 
        var response = await _client.PutAsync<ProblemDetails>(NotFoundUrl, request, HttpStatusCode.NotFound);

        // Assert 
        response.ShouldBeNotFound<Partner>(NonExistentEntityId);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var supplierId = await CreateSupplierAsync();
        var request = CreateInvalidRequest(supplierId);
        var url = GetUrl(supplierId);

        // Act
        var response = await _client.PutAsync<ValidationProblemDetails>(url, request, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(Partner.Name), response.Errors.Keys);
    }

    private static UpdateSupplierRequest CreateValidRequest(int id) =>
        new(
            id,
            "Updated supplier name",
            "Updated address",
            "Updated email",
            "Updated company name",
            true,
            1500.00m,
            ["+998912322323"]
            );

    private static UpdateSupplierRequest CreateInvalidRequest(int id) =>
        new(
            id,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            true,
            0m,
            ["+asdsgasd"]
            );
}
