using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable(nameof(Inventory));

        builder
            .HasMany(ii => ii.InventoryItems)
            .WithOne(i => i.Inventory)
            .HasForeignKey(ii => ii.InventoryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Navigation(i => i.InventoryItems)
            .AutoInclude();

        builder.HasKey(i => i.Id);

        builder
            .Property(i => i.Name)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(i => i.Location)
            .HasMaxLength(ConfigurationConstants.MaxStringLength);
    }
}
