using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable(nameof(Tenant));

        builder.HasKey(o => o.Id);

        builder
            .Property(o => o.Name)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();
    }
}
