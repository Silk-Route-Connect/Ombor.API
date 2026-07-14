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
            .HasMany(t => t.Lines)
            .WithOne(l => l.Transaction)
            .HasForeignKey(l => l.TransactionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(t => t.Warehouse)
            .WithMany()
            .HasForeignKey(t => t.WarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasOne(t => t.OriginalTransaction)
            .WithMany()
            .HasForeignKey(t => t.OriginalTransactionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .Property(t => t.RefundReason)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(t => t.Notes)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(t => t.Number)
            .IsRequired(false);

        // The document number is the dispute trail's human handle: unique per organization so concurrent
        // creates can't mint the same value (filtered to skip the nullable column's single-NULL rule).
        builder
            .HasIndex(t => new { t.OrganizationId, t.Number })
            .IsUnique()
            .HasFilter("[Number] IS NOT NULL");

        builder.Ignore(t => t.UnpaidAmount);

        builder
            .Property(t => t.TotalDue)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(t => t.TotalPaid)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(t => t.DateUtc)
            .IsRequired();

        builder
            .Property(t => t.DueDate)
            .IsRequired(false);

        builder
            .Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();

        builder
            .Property(t => t.Type)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();

        builder.HasIndex(t => t.Status);
    }
}
