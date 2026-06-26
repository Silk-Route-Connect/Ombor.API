using Ombor.Application.Interfaces;
using Ombor.Application.Localization;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;

namespace Ombor.Application.Services;

internal sealed class OrganizationSetupService(
    IApplicationDbContext context,
    IOrganizationAccessor organizationAccessor) : IOrganizationSetupService
{
    private readonly record struct StarterNames(string Category, string Wallet, string Partner, string Warehouse);

    // The starter rows are named in the language the user registered in. A small fixed table, not a general
    // i18n system; the names are ordinary editable values the user can rename or delete afterwards.
    private static readonly IReadOnlyDictionary<string, StarterNames> NamesByLanguage =
        new Dictionary<string, StarterNames>(StringComparer.Ordinal)
        {
            [SupportedLanguages.Russian] = new("Без категории", "Касса", "Розничный покупатель", "Основной склад"),
            [SupportedLanguages.UzbekLatin] = new("Turkumsiz", "Kassa", "Chakana mijoz", "Asosiy ombor"),
            [SupportedLanguages.UzbekCyrillic] = new("Туркумсиз", "Касса", "Чакана мижоз", "Асосий омбор"),
        };

    public async Task SeedStarterDataAsync(int organizationId, string language)
    {
        if (!NamesByLanguage.TryGetValue(language, out var names))
        {
            // The caller validates the language before reaching here; a miss is a programming error,
            // never a silent fallback to a default locale.
            throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported organization-setup language.");
        }

        // Pin the new organization so the starter rows are stamped to it on save (there is no
        // JWT during registration). They are ordinary rows — no system flag (rule 42).
        organizationAccessor.SetOrganization(organizationId);

        context.Categories.Add(new Category { Name = names.Category });
        context.Partners.Add(new Partner
        {
            Name = names.Partner,
            // Both, so the single starter partner is usable for sales and supplies (rule 42).
            Type = PartnerType.Both,
            OpeningDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.UtcDateTime),
        });
        context.Warehouses.Add(new Warehouse { Name = names.Warehouse });
        context.Wallets.Add(new Wallet
        {
            Name = names.Wallet,
            Type = WalletType.Cash,
            OpeningBalance = 0m,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await context.SaveChangesAsync();
    }
}
