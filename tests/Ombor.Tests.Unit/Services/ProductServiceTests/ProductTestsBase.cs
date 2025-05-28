using Moq;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public abstract class ProductTestsBase : ServiceTestsBase
{
    protected const int ProductId = 1_000; // ID to be used in GetById, Update, Delete Tests
    private protected readonly ProductService _service;
    protected readonly Product[] _defaultProducts;

    protected readonly Mock<IFileUploadService> _mockFileService;

    protected ProductTestsBase()
    {
        _defaultProducts = CreateRandomProducts();
        SetupProducts(_defaultProducts);

        _mockFileService = new Mock<IFileUploadService>();
        _service = new ProductService(_mockContext.Object, _mockValidator.Object, _mockFileService.Object);
    }

    protected override void VerifyNoOtherCalls()
    {
        _mockFileService.VerifyNoOtherCalls();
        base.VerifyNoOtherCalls();
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

    protected (Product Product, Category Category) CreateProductWithCategory()
    {
        var category = _builder.CategoryBuilder
            .BuildAndPopulate();

        var product = _builder.ProductBuilder
            .WithId(ProductId)
            .WithCategory(category)
            .BuildAndPopulate();

        return (product, category);
    }
}
