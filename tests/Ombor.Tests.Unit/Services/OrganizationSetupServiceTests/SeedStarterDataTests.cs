using Moq;
using Ombor.Application.Interfaces;
using Ombor.Application.Services;
using Ombor.Domain.Entities;

namespace Ombor.Tests.Unit.Services.OrganizationSetupServiceTests;

public sealed class SeedStarterDataTests : ServiceTestsBase
{
    [Fact]
    public async Task SeedStarterDataAsync_ShouldPinOrganization_AndCreateTheFourStarterRows()
    {
        // Arrange
        const int organizationId = 42;
        var mockAccessor = new Mock<IOrganizationAccessor>();
        var categories = SetupCategories([]);
        var partners = SetupPartners([]);
        var warehouses = SetupWarehouses([]);
        var wallets = SetupWallets([]);
        _mockContext.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(4);

        var service = new OrganizationSetupService(_mockContext.Object, mockAccessor.Object);

        // Act
        await service.SeedStarterDataAsync(organizationId);

        // Assert — the new organization is pinned, then exactly one of each starter row is added and saved.
        mockAccessor.Verify(a => a.SetOrganization(organizationId), Times.Once);
        categories.Verify(s => s.Add(It.IsAny<Category>()), Times.Once);
        partners.Verify(s => s.Add(It.IsAny<Partner>()), Times.Once);
        warehouses.Verify(s => s.Add(It.IsAny<Warehouse>()), Times.Once);
        wallets.Verify(s => s.Add(It.IsAny<Wallet>()), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
