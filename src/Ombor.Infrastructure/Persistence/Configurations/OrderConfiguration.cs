using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(nameof(Order));

        builder.HasKey(o => o.Id);

        builder.HasMany(o => o.Lines)
            .WithOne(ol => ol.Order)
            .HasForeignKey(ol => ol.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Intended warehouse — non-binding, so a warehouse that an order points at cannot be hard-deleted.
        builder.HasOne(o => o.Warehouse)
            .WithMany()
            .HasForeignKey(o => o.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // The Sale this order was promoted into on delivery (set in M5b).
        builder.HasOne(o => o.Sale)
            .WithMany()
            .HasForeignKey(o => o.SaleId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasMany(o => o.History)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.ComplexProperty(o => o.DeliveryAddress, address =>
        {
            address.Property(a => a.Text)
                .HasMaxLength(ConfigurationConstants.MaxStringLength)
                .IsRequired(false);

            // Coordinates are dormant (reserved for geo delivery) — nullable, never 0-sentinel.
            address.Property(a => a.Latitude)
                .HasPrecision(9, 6)
                .IsRequired(false);

            address.Property(a => a.Longitude)
                .HasPrecision(9, 6)
                .IsRequired(false);
        });

        builder
            .Property(o => o.OrderNumber)
            .HasMaxLength(ConfigurationConstants.OrderNumberLength)
            .IsRequired();

        builder
            .Property(o => o.TotalAmount)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(o => o.DateUtc)
            .IsRequired();

        builder
            .Property(o => o.Status)
            .HasEnumConversion()
            .IsRequired();

        builder
            .Property(o => o.Source)
            .HasEnumConversion()
            .IsRequired();
    }
}
