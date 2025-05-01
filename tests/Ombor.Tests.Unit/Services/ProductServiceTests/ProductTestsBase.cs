using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public abstract class ProductTestsBase : ServiceTestsBase
{
    protected const int ProductId = 100; // ID to be used in GetById, Update, Delete Tests
    private protected readonly ProductService _service;
    protected readonly Product[] _defaultProducts;

    protected ProductTestsBase()
    {
        _defaultProducts = CreateRandomProducts();
        SetupProducts(_defaultProducts);

        _service = new ProductService(_mockContext.Object, _mockValidator.Object);
    }

    protected Mock<DbSet<Product>> SetupProducts(IEnumerable<Product> products)
    {
        var mockSet = products.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(c => c.Products).Returns(mockSet.Object);

        return mockSet;
    }

    protected Product[] CreateRandomProducts(int count = 5)
    {
        var categories = Enumerable.Range(1, count)
            .Select(i => _builder.CategoryBuilder
                .WithId(i + 1)
                .Build());

        return Enumerable.Range(0, count)
            .Select(i => _builder.ProductBuilder
                .WithId(i + 1)
                .WithCategory(_faker.PickRandom(categories))
                .BuildAndPopulate())
            .ToArray();
    }
}
