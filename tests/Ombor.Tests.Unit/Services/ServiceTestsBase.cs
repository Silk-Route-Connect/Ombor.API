using Moq;
using Ombor.Application.Interfaces;

namespace Ombor.Tests.Unit.Services;

public abstract class ServiceTestsBase : UnitTestsBase
{
    protected readonly Mock<IRequestValidator> _mockValidator;
    protected readonly Mock<IApplicationDbContext> _mockContext;

    protected ServiceTestsBase()
    {
        _mockValidator = new Mock<IRequestValidator>();
        _mockContext = new Mock<IApplicationDbContext>();
    }

    public static TheoryData<string?> EmptySearchTerms => new()
    {
        { null },
        { "" },
        { " " },
        { "    " },
    };
}
