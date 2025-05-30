﻿using System.ComponentModel.DataAnnotations;
using Moq;
using Ombor.Contracts.Requests.Category;
using Ombor.Domain.Entities;
using Ombor.Domain.Exceptions;
using Ombor.Tests.Common.Helpers;

namespace Ombor.Tests.Unit.Services.CategoryServiceTests;

public sealed class GetCategoryByIdTests : CategoryTestsBase
{
    [Fact]
    public async Task GetByIdAsync_ShouldThrowValidationException_WhenValidatorFails()
    {
        // Arrange
        var request = new GetCategoryByIdRequest(CategoryId);

        _mockValidator.Setup(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("Validation errors."));

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        var request = new GetCategoryByIdRequest(NonExistentEntityId);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException<Category>>(
            () => _service.GetByIdAsync(request));

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Categories, Times.Once);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnDto_WhenCategoryIsFound()
    {
        // Arrange
        var expected = CreateCategory();
        var request = new GetCategoryByIdRequest(expected.Id);

        SetupCategories([.. _defaultCategories, expected]);

        // Act
        var response = await _service.GetByIdAsync(request);

        // Assert
        CategoryAssertionHelper.AssertEquivalent(expected, response);

        _mockValidator.Verify(mock => mock.ValidateAndThrowAsync(request, It.IsAny<CancellationToken>()), Times.Once);
        _mockContext.Verify(mock => mock.Categories, Times.Once);

        VerifyNoOtherCalls();
    }
}
