using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Factories;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.PartnerServiceTests;

public sealed class UpdatePartnerTests : PartnerTestsBase
{
    [Fact]
    public async Task UpdateAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange 
        var request = PartnerRequestFactory.GenerateInvalidUpdateRequest(partnerId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors"));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
             () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNorFoundException_WhenpartnerDoesNotExist()
    {
        // Arrange
        var request = PartnerRequestFactory.GenerateValidUpdateRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Partner>>(
            () => _service.UpdateAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Partners, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedpartner_WhenRequestIsValid()
    {
        // Arrange
        var partnerToUpdate = CreatePartner();
        var request = PartnerRequestFactory.GenerateValidUpdateRequest(partnerToUpdate.Id);

        SetupPartners([.. _defaultpartners, partnerToUpdate]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(1);

        // Act
        var response = await _service.UpdateAsync(request);

        // Assert
        PartnerAssertionHelper.AssertEquivalent(request, response);
        PartnerAssertionHelper.AssertEquivalent(request, partnerToUpdate);
        PartnerAssertionHelper.AssertEquivalent(partnerToUpdate, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Partners, Times.Once);

        VerifyNoOtherCalls();
    }
}
