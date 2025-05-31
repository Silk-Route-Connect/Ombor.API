using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
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

    protected virtual void VerifyNoOtherCalls()
    {
        _mockValidator.VerifyNoOtherCalls();
        _mockContext.VerifyNoOtherCalls();
    }

    protected Mock<DbSet<Category>> SetupCategories(IEnumerable<Category> categories)
    {
        var shuffledCategories = categories.ToArray();
        Random.Shared.Shuffle(shuffledCategories);

        var mockDbSet = shuffledCategories.AsQueryable()
            .BuildMockDbSet();
        _mockContext.Setup(mock => mock.Categories)
            .Returns(mockDbSet.Object);

        return mockDbSet;
    }

    protected Mock<DbSet<Product>> SetupProducts(IEnumerable<Product> products)
    {
        var mockSet = products.AsQueryable()
            .BuildMockDbSet();
        _mockContext.Setup(mock => mock.Products)
            .Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<ProductImage>> SetupProductImages(IEnumerable<ProductImage> productImages)
    {
        var mockDbSet = productImages.AsQueryable()
            .BuildMockDbSet();
        _mockContext.Setup(mock => mock.ProductImages)
            .Returns(mockDbSet.Object);

        return mockDbSet;
    }
}
