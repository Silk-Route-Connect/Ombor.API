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
    private readonly Mock<ICategoryService> _mockCategoryService;
    private readonly CategoriesController _controller;

    public CategoriesControllerTests()
    {
        _mockCategoryService = new Mock<ICategoryService>();
        _controller = new CategoriesController(_mockCategoryService.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WhenCategoriesExist()
    {
        // Arrange
        var request = _fixture.Create<GetCategoriesRequest>();
        var expected = _fixture.CreateArray<CategoryDto>();

        _mockCategoryService.Setup(s => s.GetAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockCategoryService.Verify(
            s => s.GetAsync(It.IsAny<GetCategoriesRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WithEmptyArray_WhenNoCategories()
    {
        // Arrange
        var request = _fixture.Create<GetCategoriesRequest>();
        var expected = Array.Empty<CategoryDto>();

        _mockCategoryService.Setup(s => s.GetAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockCategoryService.Verify(
            s => s.GetAsync(It.IsAny<GetCategoriesRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetCategoriesRequest>();
        var expected = _fixture.CreateException();

        _mockCategoryService.Setup(s => s.GetAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.GetAsync(request));
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ShouldReturnOkResult_WhenCategoryExists()
    {
        // Arrange
        var request = _fixture.Create<GetCategoryByIdRequest>();
        var expected = _fixture.Create<CategoryDto>();

        _mockCategoryService.Setup(s => s.GetByIdAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetCategoryByIdAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockCategoryService.Verify(
            s => s.GetByIdAsync(It.IsAny<GetCategoryByIdRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetCategoryByIdRequest>();
        var expected = _fixture.CreateException();

        _mockCategoryService.Setup(s => s.GetByIdAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.GetCategoryByIdAsync(request));
    }

    [Fact]
    public async Task PostAsync_ShouldReturnCreatedAtAction_WhenServiceReturnsCreatedCategory()
    {
        // Arrange
        var request = _fixture.Create<CreateCategoryRequest>();
        var expected = _fixture.Create<CreateCategoryResponse>();

        _mockCategoryService.Setup(s => s.CreateAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.PostAsync(request);

        // Assert
        var actual = Assert.IsType<CreatedAtActionResult>(response.Result);

        Assert.Equal(expected, actual.Value);
        Assert.NotNull(actual.RouteValues);
        Assert.Equal(expected.Id, actual.RouteValues["id"]);

        _mockCategoryService.Verify(
            s => s.CreateAsync(It.IsAny<CreateCategoryRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<CreateCategoryRequest>();
        var expected = _fixture.CreateException();

        _mockCategoryService.Setup(s => s.CreateAsync(request))
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

        _mockCategoryService.Setup(s => s.UpdateAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.PutAsync(expected.Id, request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.NotNull(actual);
        Assert.Equal(expected, actual.Value);

        _mockCategoryService.Verify(
            s => s.UpdateAsync(It.IsAny<UpdateCategoryRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task PutAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var expected = _fixture.CreateException();
        var request = _fixture.Create<UpdateCategoryRequest>();

        _mockCategoryService.Setup(s => s.UpdateAsync(It.IsAny<UpdateCategoryRequest>()))
            .ThrowsAsync(expected);

        // Act
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.PutAsync(request.Id, request));
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnNoContent_WhenRequestIsValid()
    {
        // Arrange
        var request = _fixture.Create<DeleteCategoryRequest>();

        _mockCategoryService.Setup(s => s.DeleteAsync(request))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _controller.DeleteAsync(request);

        // Assert
        Assert.IsType<NoContentResult>(response);

        _mockCategoryService.Verify(
            s => s.DeleteAsync(It.IsAny<DeleteCategoryRequest>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<DeleteCategoryRequest>();
        var expected = _fixture.CreateException();

        _mockCategoryService.Setup(s => s.DeleteAsync(It.IsAny<DeleteCategoryRequest>()))
            .Throws(expected);

        // Act & Assert
        await Assert.ThrowsAsync(
            expected.GetType(),
            () => _controller.DeleteAsync(request));
    }
}
