using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class InventoryItemConfigurarion : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable(nameof(InventoryItem));

        builder
            .HasOne(ii => ii.Inventory)
            .WithMany(i => i.InventoryItems)
            .HasForeignKey(x => x.InventoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(i => i.Product)
            .WithMany(p => p.InventoryItems)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Quantity)
            .IsRequired();
    }
}
