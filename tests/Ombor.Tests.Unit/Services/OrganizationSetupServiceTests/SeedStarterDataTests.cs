using Moq;
using Ombor.Application.Interfaces;
using Ombor.Application.Services;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Tests.Unit.Services.OrganizationSetupServiceTests;

public sealed class SeedStarterDataTests : ServiceTestsBase
{
    [Theory]
    [InlineData("ru", "Без категории", "Касса", "Розничный покупатель", "Основной склад")]
    [InlineData("uz-Latn", "Turkumsiz", "Kassa", "Chakana mijoz", "Asosiy ombor")]
    [InlineData("uz-Cyrl", "Туркумсиз", "Касса", "Чакана мижоз", "Асосий омбор")]
    public async Task SeedStarterDataAsync_ShouldPinOrganization_AndCreateLocalizedStarterRows(
        string language, string category, string wallet, string partner, string warehouse)
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
        await service.SeedStarterDataAsync(organizationId, language);

        // Assert — the new organization is pinned, then one of each starter row is added with the
        // registration language's name; the starter partner is Both (usable for sales and supplies).
        mockAccessor.Verify(a => a.SetOrganization(organizationId), Times.Once);
        categories.Verify(s => s.Add(It.Is<Category>(c => c.Name == category)), Times.Once);
        partners.Verify(s => s.Add(It.Is<Partner>(p => p.Name == partner && p.Type == PartnerType.Both)), Times.Once);
        warehouses.Verify(s => s.Add(It.Is<Warehouse>(w => w.Name == warehouse)), Times.Once);
        wallets.Verify(s => s.Add(It.Is<Wallet>(w => w.Name == wallet && w.Type == WalletType.Cash)), Times.Once);
        _mockContext.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
