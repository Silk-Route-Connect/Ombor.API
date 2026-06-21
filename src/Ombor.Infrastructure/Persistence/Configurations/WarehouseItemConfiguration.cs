using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseItemConfiguration : IEntityTypeConfiguration<WarehouseItem>
{
    public void Configure(EntityTypeBuilder<WarehouseItem> builder)
    {
        builder.ToTable(nameof(WarehouseItem));

        builder.HasKey(x => x.Id);

        builder
            .HasOne(ii => ii.Warehouse)
            .WithMany(i => i.WarehouseItems)
            .HasForeignKey(x => x.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(i => i.Product)
            .WithMany(p => p.WarehouseItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(x => x.Quantity)
            .IsRequired();

        builder
            .Property(x => x.AverageCost)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .HasIndex(x => new { x.WarehouseId, x.ProductId })
            .IsUnique();
    }
}
