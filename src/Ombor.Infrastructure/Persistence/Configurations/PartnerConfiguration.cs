using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable(nameof(Partner));

        builder.HasKey(p => p.Id);

        builder
            .HasMany(p => p.Transactions)
            .WithOne(t => t.Partner)
            .HasForeignKey(t => t.PartnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasMany(partner => partner.Payments)
            .WithOne(payment => payment.Partner)
            .HasForeignKey(payment => payment.PartnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .HasIndex(p => p.Name)
            .IsUnique();

        builder
            .Property(p => p.Name)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(p => p.Address)
            .HasMaxLength(ConfigurationConstants.MaxStringLength);

        builder
            .Property(p => p.Email)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength);

        builder
            .Property(p => p.CompanyName)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength);

        builder
            .Property(p => p.Balance)
            .HasCurrencyPrecision();

        builder
            .Property(p => p.Type)
            .HasConversion<string>()
            .IsRequired();

        builder
            .Property(p => p.PhoneNumbers)
            .HasColumnType(ConfigurationConstants.VarcharMax);
    }
}
