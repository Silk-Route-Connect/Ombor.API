using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable(nameof(Payment));

        builder.HasKey(p => p.Id);

        builder
            .HasOne(payment => payment.Partner)
            .WithMany(partner => partner.Payments)
            .HasForeignKey(payment => payment.PartnerId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);

        builder
            .HasMany(p => p.Allocations)
            .WithOne(pa => pa.Payment)
            .HasForeignKey(pa => pa.PaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasMany(p => p.Attachments)
            .WithOne(pa => pa.Payment)
            .HasForeignKey(pa => pa.PaymentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        #region Properties

        builder
            .Property(p => p.Notes)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(p => p.ExternalReference)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);

        builder
            .Property(p => p.Amount)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(p => p.AmountLocal)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(p => p.ExchangeRate)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(p => p.Type)
            .HasEnumConversion();

        builder
            .Property(p => p.Method)
            .HasEnumConversion();

        builder
            .Property(p => p.Direction)
            .HasEnumConversion();

        builder
            .Property(p => p.Currency)
            .HasEnumConversion();

        #endregion

        #region Indexes

        builder.HasIndex(p => p.DateUtc);

        builder.HasIndex(p => new { p.PartnerId, p.Type });

        #endregion
    }
}
