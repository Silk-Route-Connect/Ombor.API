using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> builder)
    {
        builder.ToTable(nameof(PaymentAllocation));

        builder.HasKey(pa => pa.Id);

        builder
            .HasOne(pa => pa.Payment)
            .WithMany(p => p.Allocations)
            .HasForeignKey(pa => pa.PaymentId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasOne(pa => pa.Transaction)
            .WithMany()
            .HasForeignKey(pa => pa.TransactionId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        #region Properties

        builder
            .Property(pa => pa.AppliedAmount)
            .HasCurrencyPrecision();

        builder
            .Property(pa => pa.Type)
            .HasEnumConversion();

        #endregion

        #region Indexes

        builder.HasIndex(pa => new { pa.PaymentId, pa.Type });

        #endregion
    }
}
