using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Extensions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.PartnerServiceTests;

public sealed class CreatePartnerTests : PartnerTestsBase
{
    [Fact]
    public async Task CreateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = PartnerRequestFactory.GenerateInvalidCreateRequest();

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert 
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnCreatedpartner_WhenRequestIsValid()
    {
        // Arrange
        var request = PartnerRequestFactory.GenerateValidCreateRequest();
        Partner addedpartner = null!;

        _mockContext.Setup(mock => mock.Partners.Add(It.Is<Partner>(partner => partner.IsEquivalent(request))))
            .Callback<Partner>(captured => addedpartner = captured);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => addedpartner.Id = 99);

        // Act
        var response = await _service.CreateAsync(request);

        // Assert
        PartnerAssertionHelper.AssertEquivalent(request, response);
        PartnerAssertionHelper.AssertEquivalent(request, addedpartner);
        PartnerAssertionHelper.AssertEquivalent(addedpartner, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Partners.Add(addedpartner), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }
}
