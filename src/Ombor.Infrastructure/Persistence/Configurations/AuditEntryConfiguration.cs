using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable(nameof(AuditEntry));

        builder.HasKey(a => a.Id);

        builder
            .Property(a => a.EntityType)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(a => a.Action)
            .HasEnumConversion()
            .IsRequired();

        builder
            .Property(a => a.OldValues)
            .HasColumnType(ConfigurationConstants.VarcharMax);

        builder
            .Property(a => a.NewValues)
            .HasColumnType(ConfigurationConstants.VarcharMax);

        builder
            .Property(a => a.TimestampUtc)
            .IsRequired();

        builder.HasIndex(a => new { a.EntityType, a.EntityId });
    }
}
