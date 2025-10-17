using AutoFixture;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ombor.API.Controllers;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Inventory;
using Ombor.Contracts.Responses.Inventory;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Controllers;

public class InventoriesControllerTests : ControllerTestsBase
{
    private readonly Mock<IInventoryService> _mockService;
    private readonly InventoriesController _controller;

    public InventoriesControllerTests()
    {
        _mockService = new Mock<IInventoryService>(MockBehavior.Strict);
        _controller = new InventoriesController(_mockService.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WhenInventoriesExist()
    {
        // Arrange
        var request = _fixture.Create<GetInventoriesRequest>();
        var expected = _fixture.CreatePagedList<InventoryDto>();

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
    public async Task GetAsync_ShouldReturnOkResult_WhitEmptyArray_WhenNoInventories()
    {
        // Arrange
        var request = _fixture.Create<GetInventoriesRequest>();
        var expected = _fixture.CreateEmptyPagedList<InventoryDto>();

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
        var request = _fixture.Create<GetInventoriesRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetAsync(request));

        _mockService.Verify(mock => mock.GetAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOkResult_WhenInventoryExists()
    {
        // Arrange
        var request = _fixture.Create<GetInventoryByIdRequest>();
        var expected = _fixture.Create<InventoryDto>();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetInventoryByIdAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetInventoryByIdRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetInventoryByIdAsync(request));

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnCreatedAtAction_WhenServiceReturnsCreatedInventory()
    {
        // Arrange
        var request = _fixture.Create<CreateInventoryRequest>();
        var expected = _fixture.Create<CreateInventoryResponse>();

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
        var request = _fixture.Create<CreateInventoryRequest>();
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
        var request = _fixture.Build<UpdateInventoryRequest>()
            .With(r => r.Id, id - 1)
            .Create();

        // Act
        var response = await _controller.PutAsync(id, request);

        // Assert
        var actual = Assert.IsType<BadRequestObjectResult>(response.Result);
        var value = actual.Value as ProblemDetails;

        Assert.NotNull(value);
        Assert.Equal("Id mismatch", value.Title);
        Assert.Equal($"Route Id ({id}) does not match body Id ({request.Id}).", value.Detail);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnOkResult_WhenServiceReturnsUpdatedInventories()
    {
        // Arrange
        var expected = _fixture.Create<UpdateInventoryResponse>();
        var request = _fixture.Build<UpdateInventoryRequest>()
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
        var request = _fixture.Create<UpdateInventoryRequest>();
        var expected = _fixture.CreateException();

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
        var request = _fixture.Create<DeleteInventoryRequest>();

        _mockService.Setup(mock => mock.DeleteAsync(request))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _controller.DeleteAsync(request);

        // Assert
        Assert.IsType<NoContentResult>(response);

        _mockService.Verify(mock => mock.DeleteAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowException_WhenServiceThorws()
    {
        // Arrange
        var request = _fixture.Create<DeleteInventoryRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.DeleteAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteAsync(request));

        _mockService.Verify(mock => mock.DeleteAsync(request), Times.Once);
    }
}