using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable(nameof(Wallet));

        builder.HasKey(w => w.Id);

        builder
            .HasIndex(w => new { w.OrganizationId, w.Name })
            .IsUnique();

        builder
            .HasMany(w => w.OutgoingTransfers)
            .WithOne(t => t.FromWallet)
            .HasForeignKey(t => t.FromWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(w => w.IncomingTransfers)
            .WithOne(t => t.ToWallet)
            .HasForeignKey(t => t.ToWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        #region Properties

        builder
            .Property(w => w.Name)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(w => w.Type)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();

        builder
            .Property(w => w.OpeningBalance)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(w => w.CreatedBy)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);

        #endregion
    }
}
