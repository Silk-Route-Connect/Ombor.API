using Ombor.Application.Interfaces;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class OrganizationSetupService(
    IApplicationDbContext context,
    IOrganizationAccessor organizationAccessor) : IOrganizationSetupService
{
    public async Task SeedStarterDataAsync(int organizationId)
    {
        // Pin the new organization so the starter rows are stamped to it on save (there is no
        // JWT during registration). They are ordinary rows — no system flag (rule 42).
        organizationAccessor.SetOrganization(organizationId);

        context.Categories.Add(new Category { Name = "Основная" });
        context.Partners.Add(new Partner
        {
            Name = "Розничный покупатель",
            Type = PartnerType.Customer,
            OpeningDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime),
        });
        context.Inventories.Add(new Inventory { Name = "Основной склад", IsActive = true });
        context.Wallets.Add(new Wallet
        {
            Name = "Касса",
            Type = WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await context.SaveChangesAsync();
    }
}
