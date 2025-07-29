using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.PartnerServiceTests;

public sealed class GetPartnerByIdTests : PartnerTestsBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new GetPartnerByIdRequest(partnerId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenpartnerDoesNotExist()
    {
        // Arrange 
        var request = new GetPartnerByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Partner>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Partners, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenpartnerIsFound()
    {
        // Arrange
        var expected = CreatePartner();
        var request = new GetPartnerByIdRequest(expected.Id);

        SetupPartners([.. _defaultpartners, expected]);

        // Act 
        var response = await _service.GetByIdAsync(request);

        // Assert
        PartnerAssertionHelper.AssertEquivalent(expected, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Partners, Times.Once);

        VerifyNoOtherCalls();
    }
}
