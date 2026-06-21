using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class OpeningStockConfiguration : IEntityTypeConfiguration<OpeningStock>
{
    public void Configure(EntityTypeBuilder<OpeningStock> builder)
    {
        builder.ToTable(nameof(OpeningStock));

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

        builder.Property(x => x.Quantity).IsRequired();

        builder.Property(x => x.UnitCost)
            .HasCurrencyPrecision()
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);
    }
}
