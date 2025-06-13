using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.SupplierServiceTests;

public sealed class CreateSupplierTests : SupplierTestsBase
{
    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = SupplierRequestFactory.GenerateInvalidCreateRequest();

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert 
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedSupplier_WhenRequestIsValid()
    {
        // Arrange
        var request = SupplierRequestFactory.GenerateValidCreateRequest();
        Partner addedSupplier = null!;

        _mockContext.Setup(mock => mock.Suppliers.Add(It.Is<Partner>(supplier => supplier.IsEquivalent(request))))
            .Callback<Partner>(captured => addedSupplier = captured);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => addedSupplier.Id = 99);

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        SupplierAssertionHelper.AssertEquivalent(request, response);
        SupplierAssertionHelper.AssertEquivalent(request, addedSupplier);
        SupplierAssertionHelper.AssertEquivalent(addedSupplier, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Suppliers.Add(addedSupplier), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
