using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable(nameof(Supplier));

        builder.HasKey(s => s.Id);

        builder
            .Property(s => s.Name)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(s => s.Address)
            .HasMaxLength(ConfigurationConstants.MaxStringLength);

        builder
            .Property(s => s.Email)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength);

        builder
            .Property(s => s.CompanyName)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength);

        builder
            .Property(s => s.IsActive)
            .IsRequired();

        builder
            .Property(s => s.Balance)
            .HasCurrencyPrecision();
    }
}
