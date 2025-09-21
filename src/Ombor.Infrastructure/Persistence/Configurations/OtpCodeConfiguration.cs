using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable(nameof(OtpCode));

        builder.HasKey(oc => oc.Id);

        builder
            .Property(oc => oc.PhoneNumber)
            .HasMaxLength(ConfigurationConstants.PhoneNumberLength)
            .IsRequired();

        builder
            .Property(oc => oc.Code)
            .HasMaxLength(ConfigurationConstants.OtpCodeLength)
            .IsRequired();
    }
}
