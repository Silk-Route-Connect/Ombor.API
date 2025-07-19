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
            .HasMany(p => p.Images)
            .WithOne(pi => pi.Product)
            .HasForeignKey(pi => pi.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(p => p.TemplateItems)
            .WithOne(ti => ti.Product)
            .HasForeignKey(ti => ti.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(p => p.Lines)
            .WithOne(tl => tl.Product)
            .HasForeignKey(tl => tl.ProductId)
            .HasForeignKey(tl => tl.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .Navigation(x => x.Category)
            .AutoInclude();

        builder
            .Navigation(x => x.Images)
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
