using System.Net;
using Microsoft.AspNetCore.Mvc;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Integration.Helpers;
using Xunit.Abstractions;

namespace Ombor.Tests.Integration.Endpoints.ProductEndpoints;

public class CreateProductTests(TestingWebApplicationFactory factory, ITestOutputHelper outputHelper)
    : ProductTestsBase(factory, outputHelper)
{
    public static readonly TheoryData<CreateProductRequest> ValidRequest = new()
    {
        ProductRequestFactory.GenerateValidCreateRequestWithoutAttachments(DefaultCategoryId, "product-1"),
        ProductRequestFactory.GenerateValidCreateRequestWithAttachments(DefaultCategoryId, "product-2")
    };

    [Theory]
    [MemberData(nameof(ValidRequest))]
    public async Task CreateAsync_ShouldReturnCreated_WhenRequestIsValid(CreateProductRequest request)
    {
        // Arrange
        var multiartForm = request.ToMultipartFormData();

        // Act
        var response = await _client.PostAsync<CreateProductResponse>(GetUrl(), multiartForm);

        // Assert
        await _responseValidator.Product.ValidatePostAsync(request, response);
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnBadRequest_WhenRequestIsInvalid()
    {
        // Arrange
        var request = ProductRequestFactory.GenerateInvalidCreateRequest(DefaultCategoryId);
        var multipartForm = request.ToMultipartFormData();

        // Act
        var response = await _client.PostAsync<ValidationProblemDetails>(GetUrl(), multipartForm, HttpStatusCode.BadRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Contains(nameof(UpdateProductRequest.Name), response.Errors.Keys);
        Assert.Contains(nameof(UpdateProductRequest.SKU), response.Errors.Keys);
    }
}
