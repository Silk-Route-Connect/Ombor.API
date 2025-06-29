using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable(nameof(LedgerEntry));
        builder.HasKey(l => l.Id);

        builder
            .HasOne(l => l.Partner)
            .WithMany(p => p.LedgerEntries)
            .HasForeignKey(l => l.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .Property(l => l.Source)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(l => l.SourceId)
            .IsRequired();

        builder
            .Property(l => l.Type)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumConversionLength)
            .IsRequired();

        builder
            .Property(l => l.AmountLocal)
            .HasCurrencyPrecision();

        builder
            .Property(l => l.Notes)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder.HasIndex(l => new { l.PartnerId, l.CreatedAtUtc });
    }
}
