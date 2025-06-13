using Microsoft.AspNetCore.Mvc;
using Ombor.Domain.Entities;
using Ombor.Tests.Integration.Endpoints.ProductEndpoints;
using Ombor.Tests.Integration.Extensions;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.SupplierEndpoints;

public class DeleteSupplierTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : SupplierTestsBase(factory, outputHelper)
{
    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenSupplierExists()
    {
        // Arrange 
        var supplierToDelete = _builder.SupplierBuilder
            .WithName("Supplier To Delete")
            .Build();

        var supplierId = await CreateSupplierAsync(supplierToDelete);
        var url = GetUrl(supplierId);

        // Act
        await _client.DeleteAsync(url);

        // Assert
        await _responseValidator.Supplier.ValidateDeleteAsync(supplierId);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNotFound_WhenSupplierDoesNotExist()
    {
        // Arrange

        // Act
        var response = await _client.DeleteAsync<ProblemDetails>(NotFoundUrl, System.Net.HttpStatusCode.NotFound);

        // Assert 
        response.ShouldBeNotFound<Partner>(NonExistentEntityId);
    }
}
