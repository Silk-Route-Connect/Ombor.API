using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class PaymentComponentConfiguration : IEntityTypeConfiguration<PaymentComponent>
{
    public void Configure(EntityTypeBuilder<PaymentComponent> builder)
    {
        builder.ToTable(nameof(PaymentComponent));

        builder.HasKey(pc => pc.Id);

        builder
            .HasOne(pc => pc.Payment)
            .WithMany(p => p.Components)
            .HasForeignKey(pc => pc.PaymentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(pc => pc.Amount)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(pc => pc.ExchangeRate)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(pc => pc.Currency)
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();

        builder
            .Property(pc => pc.Method)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();
    }
}
