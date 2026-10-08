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

        builder
            .Property(a => a.ParentEntityType)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength);

        // The Activity Log reads newest-first per organization, by actor, by operation, and by entity (the
        // «История» tabs, whose child lines are found through the parent columns).
        builder.HasIndex(a => new { a.OrganizationId, a.TimestampUtc }).IsDescending(false, true);
        builder.HasIndex(a => new { a.OrganizationId, a.UserId, a.TimestampUtc });
        builder.HasIndex(a => new { a.OrganizationId, a.OperationId });
        builder.HasIndex(a => new { a.OrganizationId, a.EntityType, a.EntityId });
        builder
            .HasIndex(a => new { a.OrganizationId, a.ParentEntityType, a.ParentEntityId })
            .HasFilter($"[{nameof(AuditEntry.ParentEntityType)}] IS NOT NULL");
    }
}
