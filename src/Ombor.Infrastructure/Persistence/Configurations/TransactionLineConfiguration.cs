using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Domain.Enums;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TransactionLineConfiguration : IEntityTypeConfiguration<TransactionLine>
{
    public void Configure(EntityTypeBuilder<TransactionLine> builder)
    {
        builder.ToTable(nameof(TransactionLine));

        builder.HasKey(tl => tl.Id);

        builder
            .HasOne(tl => tl.Transaction)
            .WithMany(t => t.Lines)
            .HasForeignKey(tl => tl.TransactionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(tl => tl.Product)
            .WithMany(p => p.Lines)
            .HasForeignKey(tl => tl.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .Property(tl => tl.UnitPrice)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(tl => tl.Discount)
            .HasCurrencyPrecision()
            .IsRequired();

        // Same precision as the stock it moves (WarehouseItem, OrderLine): a 1.125 kg line must not be stored as 1.13.
        builder
            .Property(tl => tl.Quantity)
            .HasQuantityPrecision()
            .IsRequired();

        builder
            .Property(tl => tl.DiscountType)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .HasDefaultValue(DiscountType.Percentage)
            .IsRequired();

        // Currency precision like the WAC it is snapshotted from (WarehouseItem.AverageCost).
        builder
            .Property(tl => tl.UnitCost)
            .HasCurrencyPrecision();

        builder.Ignore(tl => tl.Total);
        builder.Ignore(tl => tl.Cost);
    }
}
