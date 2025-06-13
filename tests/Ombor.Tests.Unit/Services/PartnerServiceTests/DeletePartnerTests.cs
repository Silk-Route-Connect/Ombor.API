using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Partner;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services.PartnerServiceTests;

public sealed class DeletePartnerTests : PartnerTestsBase
{
    [Fact]
    public async Task DeleteAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        //Arrange
        var request = new DeletePartnerRequest(partnerId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        //Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Partners.Remove(It.IsAny<Partner>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundExceprion_WhenpartnerDoesNotExist()
    {
        // Arrange
        var request = new DeletePartnerRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Partner>>(
            () => _service.DeleteAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Partners, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovepartner_WhenpartnerExists()
    {
        // Arrange
        var partnerToDelete = CreatePartner();
        var request = new DeletePartnerRequest(partnerToDelete.Id);

        var mockSet = Setuppartners([.. _defaultpartners, partnerToDelete]);
        mockSet.Setup(mock => mock.Remove(partnerToDelete));

        // Act
        await _service.DeleteAsync(request);

        // Then
        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        mockSet.Verify(mock => mock.Remove(partnerToDelete), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Partners, Times.Exactly(2));

        VerifyNoOtherCalls();
    }

}
