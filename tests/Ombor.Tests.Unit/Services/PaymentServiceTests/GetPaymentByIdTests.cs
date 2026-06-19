using Ombor.Application.Services;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.PaymentServiceTests;

public sealed class GetPaymentByIdTests : ServiceTestsBase
{
    private readonly PaymentService _service;

    public GetPaymentByIdTests()
        => _service = new PaymentService(_mockContext.Object, _mockValidator.Object);

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenPaymentDoesNotExist()
    {
        // Arrange
        SetupPayments([]);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Payment>>(
            () => _service.GetByIdAsync(NonExistentEntityId));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenPaymentExists()
    {
        // Arrange
        var payment = new Payment
        {
            Id = 500,
            Type = PaymentType.General,
            Direction = PaymentDirection.Income,
            DateUtc = DateTimeOffset.UtcNow,
        };
        SetupPayments([payment]);

        // Act
        var dto = await _service.GetByIdAsync(payment.Id);

        // Assert
        Assert.Equal(payment.Id, dto.Id);
        Assert.Equal(payment.Type.ToString(), dto.Type);
        Assert.Equal(payment.Direction.ToString(), dto.Direction);
    }
}
