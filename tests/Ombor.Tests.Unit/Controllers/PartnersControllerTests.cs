using AutoFixture;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Ombor.API.Controllers;
using Ombor.Application.Interfaces;
using Ombor.Contracts.Requests.Partner;
using Ombor.Contracts.Responses.Partner;
using Ombor.Tests.Unit.Extensions;

namespace Ombor.Tests.Unit.Controllers;

public sealed class PartnersControllerTests : ControllerTestsBase
{
    private readonly Mock<IPartnerService> _mockService;
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly PartnersController _controller;

    public PartnersControllerTests()
    {
        _mockService = new Mock<IPartnerService>(MockBehavior.Strict);
        _mockTransactionService = new Mock<ITransactionService>(MockBehavior.Strict);
        _mockPaymentService = new Mock<IPaymentService>(MockBehavior.Strict);
        _controller = new PartnersController(_mockService.Object, _mockTransactionService.Object, _mockPaymentService.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WhenpartnersExist()
    {
        // Arrange
        var request = _fixture.Create<GetPartnersRequest>();
        var expected = _fixture.CreateArray<PartnerDto>();

        _mockService.Setup(mock => mock.GetAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetAsync(request), Times.Once());
    }

    [Fact]
    public async Task GetAsync_ShouldReturnOkResult_WhithEmptyArray_WhenNopartners()
    {
        // Arrange
        var request = _fixture.Create<GetPartnersRequest>();
        var expected = Array.Empty<PartnerDto>();

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
    public async Task GetAsync_ShouldThrowException_WhenpartnerThrows()
    {
        // Arrange 
        var request = _fixture.Create<GetPartnersRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetAsync(request));

        _mockService.Verify(mock => mock.GetAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetpartnerByIdAsync_ShouldReturnOkResult_WhenpartnerExist()
    {
        // Arrange
        var request = _fixture.Create<GetPartnerByIdRequest>();
        var expected = _fixture.Create<PartnerDto>();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ReturnsAsync(expected);

        // Act
        var response = await _controller.GetPartnerByIdAsync(request);

        // Assert
        var actual = Assert.IsType<OkObjectResult>(response.Result);

        Assert.Equal(expected, actual.Value);

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task GetpartnerByIdAsync_ShouldThrowException_WhenServiceThrows()
    {
        // Arrange
        var request = _fixture.Create<GetPartnerByIdRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.GetByIdAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.GetPartnerByIdAsync(request));

        _mockService.Verify(mock => mock.GetByIdAsync(request), Times.Once);
    }

    [Fact]
    public async Task PostAsync_ShouldReturnCreatedAtAction_WhenServiceReturnsCreatedpartner()
    {
        // Arrange
        var request = _fixture.Create<CreatePartnerRequest>();
        var expected = _fixture.Create<CreatePartnerResponse>();

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
        var request = _fixture.Create<CreatePartnerRequest>();
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
        var request = _fixture.Build<UpdatePartnerRequest>()
            .With(r => r.Id, id - 1)
            .Create();

        //Act
        var response = await _controller.PutAsync(id, request);

        // Assert
        var actual = Assert.IsType<BadRequestObjectResult>(response.Result);
        var value = actual.Value as ProblemDetails;

        Assert.NotNull(value);
        Assert.Equal("Id mismatch", value.Title);
        Assert.Equal($"Route ID ({id}) does not match body ID ({request.Id}).", value.Detail);
    }

    [Fact]
    public async Task PutAsync_ShouldReturnOkResult_WhenServiceReturnsUpdatedpartner()
    {
        // Arrange 
        var expected = _fixture.Create<UpdatePartnerResponse>();
        var request = _fixture.Build<UpdatePartnerRequest>()
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
        var request = _fixture.Create<UpdatePartnerRequest>();
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
        var request = _fixture.Create<DeletePartnerRequest>();

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
        var request = _fixture.Create<DeletePartnerRequest>();
        var expected = _fixture.CreateException();

        _mockService.Setup(mock => mock.DeleteAsync(request))
            .ThrowsAsync(expected);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.DeleteAsync(request));

        _mockService.Verify(mock => mock.DeleteAsync(request), Times.Once);
    }
}
