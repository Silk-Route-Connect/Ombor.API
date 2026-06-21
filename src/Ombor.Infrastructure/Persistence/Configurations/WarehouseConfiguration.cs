using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(nameof(Warehouse));

        builder
            .HasMany(ii => ii.WarehouseItems)
            .WithOne(i => i.Warehouse)
            .HasForeignKey(ii => ii.WarehouseId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Navigation(i => i.WarehouseItems)
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
