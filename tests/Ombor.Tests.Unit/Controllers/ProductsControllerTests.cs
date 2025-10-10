using AutoFixture;
using Microsoft.AspNetCore.Http;
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
    private readonly Mock<IProductService> _mockService;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockService = new Mock<IProductService>(MockBehavior.Strict);
        _controller = new ProductsController(_mockService.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WhenProductsExist()
    {
        // Arrange
        var request = _fixture.Create<GetProductsRequest>();
        var expected = _fixture.CreatePagedList<ProductDto>();

        _mockService.Setup(mock => mock.GetAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WithEmptyArray_WhenNoProducts()
    {
        // Arrange
        var request = _fixture.Create<GetProductsRequest>();
        var expected = _fixture.CreatePagedList<ProductDto>();

        _mockService.Setup(mock => mock.GetAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetProductsRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetAsync(request));

        _mockService.Verify(mock => mock.GetAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldReturnOkResult_WhenProductExists()
    {
        // Arrange
        var request = _fixture.Create<GetProductByIdRequest>();
        var expected = _fixture.Create<ProductDto>();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetProductByIdAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetProductByIdAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetProductByIdRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetProductByIdAsync(request));

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnCreatedAtAction_WhenServiceReturnsCreatedProduct()
    {
        // Arrange
        var request = _fixture.Create<CreateProductRequest>();
        var expected = _fixture.Create<CreateProductResponse>();

        _mockService.Setup(mock => mock.CreateAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.PostAsync(request);

        // Assert
        var actual = Assert.IsType<CreatedAtActionResult>(response.Result);

        Assert.Equal(expected, actual.Value);
        Assert.NotNull(actual.RouteValues);
        Assert.Equal(expected.Id, actual.RouteValues["id"]);

        _mockService.Verify(mock => mock.CreateAsync(request), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<CreateProductRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.CreateAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PostAsync(request));

        _mockService.Verify(mock => mock.CreateAsync(request), Times.Once);
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

        _mockService.Setup(mock => mock.UpdateAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.PutAsync(expected.Id, request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.UpdateAsync(request), Times.Once);
    }

    [Fact]
    public async Task PutAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var expected = _fixture.CreateException();
        var request = _fixture.Create<UpdateProductRequest>();

        _mockService.Setup(mock => mock.UpdateAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutAsync(request.Id, request));

        _mockService.Verify(mock => mock.UpdateAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenRequestIsValid()
    {
        // Arrange
        var request = _fixture.Create<DeleteProductRequest>();

        _mockService.Setup(mock => mock.DeleteAsync(request))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _controller.DeleteAsync(request);

        // Assert
        Assert.IsType<NoContentResult>(response);

        _mockService.Verify(mock => mock.DeleteAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<DeleteProductRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.DeleteAsync(request))
            .Throws(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteAsync(request));

        _mockService.Verify(mock => mock.DeleteAsync(request), Times.Once);
    }
}
