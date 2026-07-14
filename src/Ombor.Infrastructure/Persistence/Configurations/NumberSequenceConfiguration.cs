using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.ToTable(nameof(NumberSequence));

        builder.HasKey(s => s.Id);

        builder
            .Property(s => s.SeriesType)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();

        builder
            .Property(s => s.LastValue)
            .IsRequired();

        // One counter row per organization per series; the allocator relies on this uniqueness
        // to serialize concurrent first-allocations for the same series.
        builder
            .HasIndex(s => new { s.OrganizationId, s.SeriesType })
            .IsUnique();
    }
}
