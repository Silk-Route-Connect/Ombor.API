using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ombor.API.Controllers;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Category;
using Ombor.Contracts.Responses.Category;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Controllers;

public sealed class CategoriesControllerTests : ControllerTestsBase
{
    private readonly Mock<ICategoryService> _mockService;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _mockService = new Mock<ICategoryService>(MockBehavior.Strict);
        _controller = new CategoriesController(_mockService.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WhenCategoriesExist()
    {
        // Arrange
        var request = _fixture.Create<GetCategoriesRequest>();
        var expected = _fixture.CreateArray<CategoryDto>();

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
    public async Task GetAsync_ShouldReturnOkResult_WithEmptyArray_WhenNoCategories()
    {
        // Arrange
        var request = _fixture.Create<GetCategoriesRequest>();
        var expected = Array.Empty<CategoryDto>();

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
        var request = _fixture.Create<GetCategoriesRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetAsync(request));

        _mockService.Verify(mock => mock.GetAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ShouldReturnOkResult_WhenCategoryExists()
    {
        // Arrange
        var request = _fixture.Create<GetCategoryByIdRequest>();
        var expected = _fixture.Create<CategoryDto>();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetCategoryByIdAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetCategoryByIdRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetCategoryByIdAsync(request));

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnCreatedAtAction_WhenServiceReturnsCreatedCategory()
    {
        // Arrange
        var request = _fixture.Create<CreateCategoryRequest>();
        var expected = _fixture.Create<CreateCategoryResponse>();

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
        var request = _fixture.Create<CreateCategoryRequest>();
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
        var request = _fixture.Build<UpdateCategoryRequest>()
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
    public async Task PutAsync_ShouldReturnOkResult_WhenServiceReturnsUpdatedCategory()
    {
        // Arrange
        var expected = _fixture.Create<UpdateCategoryResponse>();
        var request = _fixture.Build<UpdateCategoryRequest>()
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
        var request = _fixture.Create<UpdateCategoryRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.UpdateAsync(request))
            .ThrowsAsync(expected);

        // Act
        await Assert.ThrowsAsync<Exception>(() => _controller.PutAsync(request.Id, request));

        _mockService.Verify(mock => mock.UpdateAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenRequestIsValid()
    {
        // Arrange
        var request = _fixture.Create<DeleteCategoryRequest>();

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
        var request = _fixture.Create<DeleteCategoryRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.DeleteAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteAsync(request));

        _mockService.Verify(mock => mock.DeleteAsync(request), Times.Once);
    }
}
