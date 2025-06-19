using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<TransactionRecord>
{
    public void Configure(EntityTypeBuilder<TransactionRecord> builder)
    {
        builder.ToTable(nameof(TransactionRecord));

        builder.HasKey(t => t.Id);

        builder
            .HasOne(t => t.Partner)
            .WithMany(p => p.Transactions)
            .HasForeignKey(t => t.PartnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasOne(t => t.RefundedTransaction)
            .WithMany(rt => rt.Refunds)
            .HasForeignKey(rt => rt.RefundedTransactionId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder
            .HasMany(t => t.Lines)
            .WithOne(l => l.Transaction)
            .HasForeignKey(l => l.TransactionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        #region Properties

        builder
            .Property(t => t.Notes)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(t => t.TransactionNumber)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);

        builder
            .Property(t => t.TotalDue)
            .HasCurrencyPrecision();

        builder
            .Property(t => t.TotalPaid)
            .HasCurrencyPrecision();

        builder
            .Property(t => t.DateUtc)
            .IsRequired();

        builder
            .Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumConversionLength)
            .IsRequired();

        builder
            .Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumConversionLength)
            .IsRequired();

        #endregion

        #region Indexes

        builder.HasIndex(t => t.DateUtc);
        builder.HasIndex(t => new { t.PartnerId, t.Type, t.Status });

        #endregion
    }
}
