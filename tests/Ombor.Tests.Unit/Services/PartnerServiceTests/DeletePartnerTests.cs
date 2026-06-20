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
    public async Task DeleteAsync_ShouldHardDeletePartner_WhenNotReferenced()
    {
        // Arrange
        var partnerToDelete = CreatePartner();
        var request = new DeletePartnerRequest(partnerToDelete.Id);

        var mockSet = SetupPartners([.. _defaultpartners, partnerToDelete]);
        SetupTransactions([]);
        SetupPayments([]);
        SetupOrders([]);
        SetupTemplates([]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(request);

        // Then — an unreferenced partner is hard-deleted.
        mockSet.Verify(mock => mock.Remove(It.Is<Partner>(e => e.Id == partnerToDelete.Id)), Times.Once);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenPartnerReferencedByTransaction()
    {
        // Arrange
        var partnerToDelete = CreatePartner();
        var request = new DeletePartnerRequest(partnerToDelete.Id);

        var mockSet = SetupPartners([.. _defaultpartners, partnerToDelete]);
        SetupTransactions([new TransactionRecord { Id = 1, PartnerId = partnerToDelete.Id, Partner = null! }]);
        SetupPayments([]);
        SetupOrders([]);
        SetupTemplates([]);

        // Act & Assert — a referenced partner cannot be hard-deleted (409).
        await Assert.ThrowsAsync<Ombor.Domain.Exceptions.ConflictException>(
            () => _service.DeleteAsync(request));

        mockSet.Verify(mock => mock.Remove(It.IsAny<Partner>()), Times.Never);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ArchiveAsync_ShouldSetIsArchived_WhenPartnerExists()
    {
        // Arrange
        var partner = CreatePartner();
        SetupPartners([.. _defaultpartners, partner]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.ArchiveAsync(partner.Id);

        // Assert
        Assert.True(partner.IsArchived);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_ShouldClearIsArchived_WhenPartnerExists()
    {
        // Arrange
        var partner = CreatePartner();
        partner.IsArchived = true;
        SetupPartners([.. _defaultpartners, partner]);

        _mockContext.Setup(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _service.RestoreAsync(partner.Id);

        // Assert
        Assert.False(partner.IsArchived);
        _mockContext.Verify(mock => mock.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
