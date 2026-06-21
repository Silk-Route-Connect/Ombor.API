using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable(nameof(StockAdjustment));

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Warehouse)
            .WithMany()
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.Property(x => x.DateUtc).IsRequired();

        builder.Property(x => x.Direction)
            .HasEnumConversion()
            .IsRequired();

        builder.Property(x => x.Quantity).IsRequired();

        builder.Property(x => x.Reason)
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder.Property(x => x.UnitCost)
            .HasCurrencyPrecision()
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);
    }
}
