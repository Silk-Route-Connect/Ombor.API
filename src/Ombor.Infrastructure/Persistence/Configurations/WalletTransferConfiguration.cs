using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class WalletTransferConfiguration : IEntityTypeConfiguration<WalletTransfer>
{
    public void Configure(EntityTypeBuilder<WalletTransfer> builder)
    {
        builder.ToTable(nameof(WalletTransfer));

        builder.HasKey(t => t.Id);

        // Relationships are configured from the Wallet side (WalletConfiguration); the FromWallet
        // and ToWallet foreign keys are Restrict so a wallet that has moved money cannot be hard-deleted.

        #region Properties

        builder
            .Property(t => t.Amount)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(t => t.Note)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(t => t.DateUtc)
            .IsRequired();

        builder
            .HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedById)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        #endregion
    }
}
