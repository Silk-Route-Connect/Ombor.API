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

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of templates.")]
    public int NumberOfTemplates { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of items per template.")]
    public int NumberOfItemsPerTemplate { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of inventories.")]
    public int NumberOfInventories { get; set; }

    [Range(1, double.MaxValue, ErrorMessage = "Invalid number of items per inventory.")]
    public int NumberOfItemsPerInventory { get; set; }
}
