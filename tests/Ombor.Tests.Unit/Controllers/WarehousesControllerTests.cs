using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ombor.API.Controllers;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Controllers;

public class WarehousesControllerTests : ControllerTestsBase
{
    private readonly Mock<IWarehouseService> _mockService;
    private readonly WarehousesController _controller;

    public WarehousesControllerTests()
    {
        _mockService = new Mock<IWarehouseService>(MockBehavior.Strict);
        _controller = new WarehousesController(_mockService.Object, Mock.Of<IMovementService>());
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WhenWarehousesExist()
    {
        // Arrange
        var request = _fixture.Create<GetWarehousesRequest>();
        var expected = _fixture.CreateArray<WarehouseDto>();

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
    public async Task GetAsync_ShouldReturnOkResult_WhitEmptyArray_WhenNoWarehouses()
    {
        // Arrange
        var request = _fixture.Create<GetWarehousesRequest>();
        var expected = Array.Empty<WarehouseDto>();

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
        var request = _fixture.Create<GetWarehousesRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetAsync(request));

        _mockService.Verify(mock => mock.GetAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnOkResult_WhenWarehouseExists()
    {
        // Arrange
        var request = _fixture.Create<GetWarehouseByIdRequest>();
        var expected = _fixture.Create<WarehouseDto>();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetWarehouseByIdAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetWarehouseByIdRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetWarehouseByIdAsync(request));

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetStockAsync_ShouldReturnOkResult_WithStockItems()
    {
        // Arrange
        var warehouseId = _fixture.Create<int>();
        var expected = _fixture.CreateArray<WarehouseStockItemDto>();

        _mockService.Setup(mock => mock.GetStockAsync(It.Is<GetWarehouseByIdRequest>(r => r.Id == warehouseId)))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetStockAsync(warehouseId);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetStockAsync(It.Is<GetWarehouseByIdRequest>(r => r.Id == warehouseId)), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnCreatedAtAction_WhenServiceReturnsCreatedWarehouse()
    {
        // Arrange
        var request = _fixture.Create<CreateWarehouseRequest>();
        var expected = _fixture.Create<WarehouseDto>();

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
        var request = _fixture.Create<CreateWarehouseRequest>();
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
        var request = _fixture.Build<UpdateWarehouseRequest>()
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
    public async Task PutAsync_ShouldReturnOkResult_WhenServiceReturnsUpdatedWarehouses()
    {
        // Arrange
        var expected = _fixture.Create<WarehouseDto>();
        var request = _fixture.Build<UpdateWarehouseRequest>()
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
        var request = _fixture.Create<UpdateWarehouseRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.UpdateAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.PutAsync(request.Id, request));

        _mockService.Verify(mock => mock.UpdateAsync(request), Times.Once);
    }

    [Fact]
    public async Task AddOpeningStockAsync_ShouldReturnBadRequest_WhenRouteIdDoesNotMatchRequest()
    {
        // Arrange
        var id = _fixture.Create<int>() + 1;
        var request = _fixture.Build<AddOpeningStockRequest>()
            .With(r => r.WarehouseId, id - 1)
            .Create();

        // Act
        var response = await _controller.AddOpeningStockAsync(id, request);

        // Assert
        var actual = Assert.IsType<BadRequestObjectResult>(response.Result);
        var value = actual.Value as ProblemDetails;

        Assert.NotNull(value);
        Assert.Equal("Id mismatch", value.Title);
        Assert.Equal($"Route Id ({id}) does not match body WarehouseId ({request.WarehouseId}).", value.Detail);
    }

    [Fact]
    public async Task AddOpeningStockAsync_ShouldReturnOkResult_WhenServiceReturnsWarehouse()
    {
        // Arrange
        var expected = _fixture.Create<WarehouseDto>();
        var request = _fixture.Build<AddOpeningStockRequest>()
            .With(r => r.WarehouseId, expected.Id)
            .Create();

        _mockService.Setup(mock => mock.AddOpeningStockAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.AddOpeningStockAsync(expected.Id, request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.AddOpeningStockAsync(request), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_ShouldReturnOkResult_WhenServiceArchivesWarehouse()
    {
        // Arrange
        var warehouseId = _fixture.Create<int>();
        var expected = _fixture.Create<WarehouseDto>();

        _mockService.Setup(mock => mock.ArchiveAsync(warehouseId))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.ArchiveAsync(warehouseId);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.ArchiveAsync(warehouseId), Times.Once);
    }

    [Fact]
    public async Task ArchiveAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var warehouseId = _fixture.Create<int>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.ArchiveAsync(warehouseId))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.ArchiveAsync(warehouseId));

        _mockService.Verify(mock => mock.ArchiveAsync(warehouseId), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_ShouldReturnOkResult_WhenServiceRestoresWarehouse()
    {
        // Arrange
        var warehouseId = _fixture.Create<int>();
        var expected = _fixture.Create<WarehouseDto>();

        _mockService.Setup(mock => mock.RestoreAsync(warehouseId))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.RestoreAsync(warehouseId);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.RestoreAsync(warehouseId), Times.Once);
    }
}
