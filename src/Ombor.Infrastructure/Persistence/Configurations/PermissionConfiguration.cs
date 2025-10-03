using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable(nameof(Permission));

        builder.HasKey(p => p.Id);

        builder.HasMany(p => p.Roles)
            .WithMany(rp => rp.Permissions);

        builder
            .Property(p => p.Module)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(p => p.Action)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(p => p.Resource)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);

        builder
            .Property(p => p.Description)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);
    }
}
