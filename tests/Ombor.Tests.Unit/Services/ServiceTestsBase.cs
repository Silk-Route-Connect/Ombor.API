using Moq;
using Ombor.Application.Interfaces;
using Ombor.Tests.Common.Builders;
using Ombor.Tests.Common.Interfaces;

namespace Ombor.Tests.Unit.Services;

public abstract class ServiceTestsBase : UnitTestsBase
{
    protected const int NonExistentEntityId = 9_999_999;

    protected readonly Mock<IRequestValidator> _mockValidator;
    protected readonly Mock<IApplicationDbContext> _mockContext;
    protected readonly ITestDataBuilder _builder;

    protected ServiceTestsBase()
    {
        _mockValidator = new Mock<IRequestValidator>();
        _mockContext = new Mock<IApplicationDbContext>();
        _builder = new TestDataBuilder();
    }

    protected void VerifyNoOtherCalls()
    {
        _mockValidator.VerifyNoOtherCalls();
        _mockContext.VerifyNoOtherCalls();
    }
}
