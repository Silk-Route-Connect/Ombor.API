using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable(nameof(OrderLine));

        builder.HasKey(ol => ol.Id);

        builder.HasOne(ol => ol.Order)
            .WithMany(o => o.Lines)
            .HasForeignKey(ol => ol.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(ol => ol.Product)
            .WithMany(p => p.OrderLines)
            .HasForeignKey(ol => ol.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(ol => ol.Quantity)
            .IsRequired();

        builder
            .Property(ol => ol.UnitPrice)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(ol => ol.Discount)
            .HasCurrencyPrecision()
            .IsRequired(false);
    }
}
