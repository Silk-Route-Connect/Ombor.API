using FluentValidation;
using FluentValidation.Results;
using Moq;
using Ombor.Application.Services;

namespace Ombor.Tests.Unit.Services;

public sealed class RequestValidatorTests
{
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly RequestValidator _validator;

    public RequestValidatorTests()
    {
        _serviceProviderMock = new Mock<IServiceProvider>();
        _validator = new RequestValidator(_serviceProviderMock.Object);
    }

    [Fact]
    public void ValidateAndThrow_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        object request = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _validator.ValidateAndThrow(request));
    }

    [Fact]
    public void ValidateAndThrow_ShouldThrowInvalidOperationException_WhenNoValidatorRegistered()
    {
        // Arrange
        var request = new object();

        _serviceProviderMock.Setup(mock => mock.GetService(typeof(IValidator<object>)))
            .Returns(null);

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => _validator.ValidateAndThrow(request));

        Assert.Contains("No service for type", exception.Message);
    }

    [Fact]
    public void ValidateAndThrow_ShouldThrowValidationException_WhenValidatorReturnsInvalidResult()
    {
        // Arrange
        var request = new object();
        var failure = new ValidationFailure("Foo", "Bar");
        var result = new ValidationResult([failure]);
        var validatorMock = new Mock<IValidator<object>>();

        _serviceProviderMock.Setup(mock => mock.GetService(typeof(IValidator<object>)))
            .Returns(validatorMock.Object);

        validatorMock.Setup(mock => mock.Validate(request))
            .Returns(result);

        // Act & Assert
        var exception = Assert.Throws<ValidationException>(() => _validator.ValidateAndThrow(request));

        Assert.Single(exception.Errors);

        var error = exception.Errors.Single();
        Assert.Equal("Foo", error.PropertyName);
        Assert.Equal("Bar", error.ErrorMessage);
    }

    [Fact]
    public void ValidateAndThrow_ShouldCompleteSuccessfully_WhenValidatorReturnsValidResult()
    {
        // Arrange
        var request = new object();
        var result = new ValidationResult();
        var validatorMock = new Mock<IValidator<object>>();

        _serviceProviderMock.Setup(mock => mock.GetService(typeof(IValidator<object>)))
            .Returns(validatorMock.Object);

        validatorMock.Setup(mock => mock.Validate(request))
            .Returns(result);

        // Act
        _validator.ValidateAndThrow(request);

        // Assert
        _serviceProviderMock.Verify(mock => mock.GetService(typeof(IValidator<object>)), Times.Once);
        validatorMock.Verify(mock => mock.Validate(request), Times.Once);

        _serviceProviderMock.VerifyNoOtherCalls();
        validatorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAndThrowAsync_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        object request = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _validator.ValidateAndThrowAsync(request));
    }

    [Fact]
    public async Task ValidateAndThrowAsync_ShouldThrowInvalidOperationException_WhenNoValidatorRegistered()
    {
        // Arrange
        var request = new object();

        _serviceProviderMock.Setup(mock => mock.GetService(typeof(IValidator<object>)))
            .Returns(null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _validator.ValidateAndThrowAsync(request));

        Assert.Contains("No service for type", exception.Message);
    }

    [Fact]
    public async Task ValidateAndThrowAsync_ShouldThrowValidationException_WhenValidatorReturnsInvalidResult()
    {
        // Arrange
        var request = new object();
        var failure = new ValidationFailure("Foo", "Bar");
        var result = new ValidationResult([failure]);
        var validatorMock = new Mock<IValidator<object>>();

        _serviceProviderMock.Setup(mock => mock.GetService(typeof(IValidator<object>)))
            .Returns(validatorMock.Object);

        validatorMock.Setup(mock => mock.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _validator.ValidateAndThrowAsync(request));

        Assert.Single(exception.Errors);

        var error = exception.Errors.Single();
        Assert.Equal("Foo", error.PropertyName);
        Assert.Equal("Bar", error.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAndThrowAsync_ShouldCompleteSuccessfully_WhenValidatorReturnsValidResult()
    {
        // Arrange
        var request = new object();
        var result = new ValidationResult();
        var validatorMock = new Mock<IValidator<object>>();

        _serviceProviderMock.Setup(mock => mock.GetService(typeof(IValidator<object>)))
            .Returns(validatorMock.Object);

        validatorMock.Setup(mock => mock.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        await _validator.ValidateAndThrowAsync(request);

        // Assert
        _serviceProviderMock.Verify(mock => mock.GetService(typeof(IValidator<object>)), Times.Once);
        validatorMock.Verify(mock => mock.ValidateAsync(request, It.IsAny<CancellationToken>()), Times.Once);

        _serviceProviderMock.VerifyNoOtherCalls();
        validatorMock.VerifyNoOtherCalls();
    }
}
