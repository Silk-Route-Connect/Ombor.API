using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ombor.API.Controllers;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Product;
using Ombor.Contracts.Responses.Product;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Controllers;

public sealed class ProductsControllerTests : ControllerTestsBase
{
    private readonly Mock<IProductService> _mockProductService;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockProductService = new Mock<IProductService>();
        _controller = new ProductsController(_mockProductService.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WhenProductsExist()
    {
        // Arrange
        var request = _fixture.Create<GetProductsRequest>();
        var expected = _fixture.CreateArray<ProductDto>();

        _mockProductService.Setup(s => s.GetAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockProductService.Verify(
            s => s.GetAsync(It.IsAny<GetProductsRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WithEmptyArray_WhenNoProducts()
    {
        // Arrange
        var request = _fixture.Create<GetProductsRequest>();
        var expected = Array.Empty<ProductDto>();

        _mockProductService.Setup(s => s.GetAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockProductService.Verify(
            s => s.GetAsync(It.IsAny<GetProductsRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetProductsRequest>();
        var expected = _fixture.CreateException();

        _mockProductService.Setup(s => s.GetAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.GetAsync(request));
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnOkResult_WhenProductExists()
    {
        // Arrange
        var request = _fixture.Create<GetProductByIdRequest>();
        var expected = _fixture.Create<ProductDto>();

        _mockProductService.Setup(s => s.GetByIdAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetProductByIdAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockProductService.Verify(
            s => s.GetByIdAsync(It.IsAny<GetProductByIdRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetProductByIdRequest>();
        var expected = _fixture.CreateException();

        _mockProductService.Setup(s => s.GetByIdAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.GetProductByIdAsync(request));
    }

    [Fact]
    public async Task PostAsync_ShouldReturnCreatedAtAction_WhenServiceReturnsCreatedProduct()
    {
        // Arrange
        var request = _fixture.Create<CreateProductRequest>();
        var expected = _fixture.Create<CreateProductResponse>();

        _mockProductService.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.PostAsync(request);

        // Assert
        var actual = Assert.IsType<CreatedAtActionResult>(response.Result);

        Assert.Equal(expected, actual.Value);
        Assert.NotNull(actual.RouteValues);
        Assert.Equal(expected.Id, actual.RouteValues["id"]);

        _mockProductService.Verify(
            s => s.CreateAsync(It.IsAny<CreateProductRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<CreateProductRequest>();
        var expected = _fixture.CreateException();

        _mockProductService.Setup(s => s.CreateAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.PostAsync(request));
    }

    [Fact]
    public async Task PutAsync_ShouldReturnBadRequest_WhenRouteIdDoesNotMatchRequest()
    {
        // Arrange
        var id = _fixture.Create<int>() + 1;
        var request = _fixture.Build<UpdateProductRequest>()
            .With(r => r.Id, id - 1)
            .Create();

        // Act
        var response = await _controller.PutAsync(id, request);

        // Assert
        var actual = Assert.IsType<BadRequestObjectResult>(response.Result);
        var value = actual.Value as ProblemDetails;

        Assert.NotNull(value);
        Assert.Equal("ID mismatch", value.Title);
        Assert.Equal($"Route ID ({id}) does not match body ID ({request.Id}).", value.Detail);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnOkResult_WhenServiceReturnsUpdatedProduct()
    {
        // Arrange
        var expected = _fixture.Create<UpdateProductResponse>();
        var request = _fixture.Build<UpdateProductRequest>()
            .With(r => r.Id, expected.Id)
            .Create();

        _mockProductService.Setup(s => s.UpdateAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.PutAsync(expected.Id, request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockProductService.Verify(
            s => s.UpdateAsync(It.IsAny<UpdateProductRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task PutAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var expected = _fixture.CreateException();
        var request = _fixture.Create<UpdateProductRequest>();

        _mockProductService.Setup(s => s.UpdateAsync(It.IsAny<UpdateProductRequest>()))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.PutAsync(request.Id, request));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenRequestIsValid()
    {
        // Arrange
        var request = _fixture.Create<DeleteProductRequest>();

        _mockProductService.Setup(s => s.DeleteAsync(request))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _controller.DeleteAsync(request);

        // Assert
        Assert.IsType<NoContentResult>(response);

        _mockProductService.Verify(
            s => s.DeleteAsync(It.IsAny<DeleteProductRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<DeleteProductRequest>();
        var expected = _fixture.CreateException();

        _mockProductService.Setup(s => s.DeleteAsync(It.IsAny<DeleteProductRequest>()))
            .Throws(expected);

        // Act & Assert
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.DeleteAsync(request));
    }
}
