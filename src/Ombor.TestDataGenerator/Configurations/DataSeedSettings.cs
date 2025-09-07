using System.ComponentModel.DataAnnotations;

namespace Ombor.TestDataGenerator.Configurations;

public sealed class DataSeedSettings
{
    public const string SectionName = nameof(DataSeedSettings);

    [Required(ErrorMessage = "Locale is required")]
    [AllowedValues(["en", "ru"], ErrorMessage = "Only 'en' and 'ru' locales are allowed")]
    public required string Locale { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of categories.")]
    public int NumberOfCategories { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of products.")]
    public int NumberOfProducts { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of images per product.")]
    public int NumberOfMaxImagesPerProduct { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of partners.")]
    public int NumberOfPartners { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of templates per partner.")]
    public int NumberOfTemplatesPerPartner { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of items per template.")]
    public int NumberOfItemsPerTemplate { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of inventories.")]
    public int NumberOfInventories { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of items per inventory.")]
    public int NumberOfItemsPerInventory { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of transactions per partner.")]
    public int NumberOfMaxTransactionsPerPartner { get; set; }

    public PaymentSeedSettings PaymentSettings { get; init; } = new PaymentSeedSettings();

}

public sealed class PaymentSeedSettings
{
    public int MaxInstallmentsPerTransaction { get; init; } = 3;
    public int MaxDaysAfterTransaction { get; init; } = 30;

    public decimal ChanceUsdComponent { get; init; } = 0.3m;
    public decimal ChanceOverpay { get; init; } = 0.15m;
    public decimal ChanceExactPay { get; init; } = 0.55m;
    public decimal ChancePartialUnpaid { get; init; } = 0.30m;
    public decimal ChanceChangeReturnVsAdvance { get; init; } = 0.30m;

    public (int MinInstallmentsCount, int MaxInstallmentsCount) InstallmentCountRange { get; init; } = (1, 3);
}