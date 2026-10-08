using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ombor.API.Controllers;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Warehouse;
using Ombor.Contracts.Responses.Warehouse;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Controllers;

/// <summary>The stock side of <see cref="WarehousesController"/>: the «Остатки» rows, opening stock and a row's threshold.</summary>
public class WarehousesControllerStockTests : ControllerTestsBase
{
    private readonly Mock<IWarehouseService> _mockService;
    private readonly WarehousesController _controller;

    public WarehousesControllerStockTests()
    {
        _mockService = new Mock<IWarehouseService>(MockBehavior.Strict);
        _controller = new WarehousesController(_mockService.Object, Mock.Of<IMovementService>());
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
    public async Task SetLowStockThresholdAsync_ShouldReturnOkResult_WithTheUpdatedRow()
    {
        // Arrange
        var request = new SetLowStockThresholdRequest(LowStockThreshold: 5m);
        var expected = _fixture.Create<WarehouseStockItemDto>();

        _mockService.Setup(mock => mock.SetLowStockThresholdAsync(3, 7, request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.SetLowStockThresholdAsync(3, 7, request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.SetLowStockThresholdAsync(3, 7, request), Times.Once);
    }
}
