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
            .HasForeignKey(o => o.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .ComplexProperty(o => o.DeliveryAddress)
            .IsRequired();

        builder
            .Property(o => o.OrderNumber)
            .HasMaxLength(ConfigurationConstants.EnumLength)
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
