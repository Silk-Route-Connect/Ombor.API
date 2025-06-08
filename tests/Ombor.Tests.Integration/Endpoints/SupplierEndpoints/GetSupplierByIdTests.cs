using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Responses.Supplier;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Endpoints.ProductEndpoints;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.SupplierEndpoints;

public class GetSupplierByIdTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
        : SupplierTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task GetByIdAsync_ShouldReturnOk_WhenSupplierExists()
    {
        // Arrange 
        var supplier = _builder.SupplierBuilder
            .WithName("Supplier To Be Fetched")
            .Build();

        var supplierId = await CreateSupplierAsync(supplier);
        var url = GetUrl(supplierId);

        // Act
        var response = await _client.GetAsync<SupplierDto>(url);

        // Assert
        await _responseValidator.Supplier.ValidateGetByIdAsync(supplierId, response);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNotFound_WhenSupplierDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.GetAsync<ProblemDetails>(NotFoundUrl, System.Net.HttpStatusCode.NotFound);

        // Assert
        response.ShouldBeNotFound<Supplier>(NonExistentEntityId);
    }
}
