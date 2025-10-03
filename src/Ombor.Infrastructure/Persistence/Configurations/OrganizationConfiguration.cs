using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable(nameof(Organization));

        builder.HasKey(o => o.Id);

        builder
            .Property(o => o.Name)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();
    }
}
