using Microsoft.Extensions.Options;
using Moq;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Services;
using Ombor.Domain.Entities;
using Ombor.Tests.Common.Factories;

namespace Ombor.Tests.Unit.Services.ProductServiceTests;

public abstract class ProductTestsBase : ServiceTestsBase
{
    private protected const int ProductId = 1_000; // ID to be used in GetById, Update, Delete Tests
    private protected readonly ProductService _service;
    private protected readonly Product[] _defaultProducts;
    private protected readonly Mock<IFileService> _mockFileService;
    private protected readonly FileSettings _fileSettings;
    private protected readonly Category _defaultCategory;

    protected ProductTestsBase()
    {
        _defaultProducts = CreateRandomProducts();
        _defaultCategory = _builder.CategoryBuilder
            .WithId(1_000)
            .BuildAndPopulate();
        SetupProducts(_defaultProducts);
        // The read + delete paths gate deletability on these (DR-20); default to unreferenced.
        SetupTransactionLines([]);
        SetupOrderLines([]);
        SetupOpeningStocks([]);
        SetupStockAdjustments([]);
        SetupTransferLines([]);
        SetupTemplateItems([]);
        SetupWarehouseItems([]);

        _fileSettings = FileSettingsFactory.CreateDefault();
        var options = Options.Create(_fileSettings);

        _mockFileService = new Mock<IFileService>();
        _service = new ProductService(_mockContext.Object, _mockValidator.Object, _mockFileService.Object, options);
    }

    /// <summary>The reference predicate reads every set that can point at a product (one source for IsDeletable and delete).</summary>
    protected void VerifyReferenceSetsRead()
    {
        _mockContext.Verify(mock => mock.TransactionLines, Times.Once);
        _mockContext.Verify(mock => mock.OrderLines, Times.Once);
        _mockContext.Verify(mock => mock.OpeningStocks, Times.Once);
        _mockContext.Verify(mock => mock.StockAdjustments, Times.Once);
        _mockContext.Verify(mock => mock.TransferLines, Times.Once);
        _mockContext.Verify(mock => mock.TemplateItems, Times.Once);
        _mockContext.Verify(mock => mock.WarehouseItems, Times.Once);
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
