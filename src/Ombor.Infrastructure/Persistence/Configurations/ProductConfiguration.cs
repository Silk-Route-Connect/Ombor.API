using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(nameof(Product));

        builder.HasKey(p => p.Id);

        builder
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Navigation(x => x.Category)
            .AutoInclude();

        builder
            .HasIndex(x => x.SKU)
            .IsUnique();

        #region Properties

        builder
            .Property(p => p.Name)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(p => p.SKU)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(p => p.Description)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(p => p.Barcode)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(p => p.SalePrice)
            .HasCurrencyPrecision();

        builder
            .Property(p => p.SupplyPrice)
            .HasCurrencyPrecision();

        builder
            .Property(p => p.RetailPrice)
            .HasCurrencyPrecision();

        builder
            .Property(p => p.QuantityInStock)
            .IsRequired();

        builder
            .Property(p => p.LowStockThreshold)
            .IsRequired();

        builder
            .Property(p => p.Measurement)
            .HasConversion<string>()
            .IsRequired();

        builder
            .Property(p => p.Type)
            .HasConversion<string>()
            .IsRequired();

        #endregion
    }
}
