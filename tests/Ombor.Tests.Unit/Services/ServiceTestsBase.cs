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

    protected Mock<DbSet<Partner>> SetupPartners(IEnumerable<Partner> partners)
    {
        var mockSet = partners.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.Partners).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<Wallet>> SetupWallets(IEnumerable<Wallet> wallets)
    {
        var mockSet = wallets.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.Wallets).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<Warehouse>> SetupWarehouses(IEnumerable<Warehouse> warehouses)
    {
        var mockSet = warehouses.AsQueryable().BuildMockDbSet();

        _mockContext.Setup(mock => mock.Warehouses).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<PartnerBalance>> SetupPartnerBalances(IEnumerable<PartnerBalance> balances)
    {
        var mockSet = balances.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.PartnerBalances).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<TransactionLine>> SetupTransactionLines(IEnumerable<TransactionLine> lines)
    {
        var mockSet = lines.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.TransactionLines).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<OrderLine>> SetupOrderLines(IEnumerable<OrderLine> lines)
    {
        var mockSet = lines.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.OrderLines).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<TransactionRecord>> SetupTransactions(IEnumerable<TransactionRecord> transactions)
    {
        var mockSet = transactions.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.Transactions).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<Payment>> SetupPayments(IEnumerable<Payment> payments)
    {
        var mockSet = payments.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.Payments).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<Order>> SetupOrders(IEnumerable<Order> orders)
    {
        var mockSet = orders.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.Orders).Returns(mockSet.Object);

        return mockSet;
    }

    protected Mock<DbSet<Template>> SetupTemplates(IEnumerable<Template> templates)
    {
        var mockSet = templates.AsQueryable().BuildMockDbSet();
        _mockContext.Setup(mock => mock.Templates).Returns(mockSet.Object);

        return mockSet;
    }
}
