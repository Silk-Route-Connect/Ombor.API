using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class OrderStatusEventConfiguration : IEntityTypeConfiguration<OrderStatusEvent>
{
    public void Configure(EntityTypeBuilder<OrderStatusEvent> builder)
    {
        builder.ToTable(nameof(OrderStatusEvent));

        builder.HasKey(e => e.Id);

        // The Order <-> History relationship is configured from the Order side (OrderConfiguration).

        builder
            .Property(e => e.At)
            .IsRequired();

        // From is null on the creation event, so it cannot use the non-nullable HasEnumConversion helper.
        builder
            .Property(e => e.From)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired(false);

        builder
            .Property(e => e.To)
            .HasEnumConversion()
            .IsRequired();

        builder
            .Property(e => e.By)
            .IsRequired(false);

        builder.HasIndex(e => e.OrderId);
    }
}
