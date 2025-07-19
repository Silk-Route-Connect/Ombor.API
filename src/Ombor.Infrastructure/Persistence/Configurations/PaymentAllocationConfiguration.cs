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
            .HasOne(pa => pa.Transaction)
            .WithMany(t => t.PaymentAllocations)
            .HasForeignKey(pa => pa.TransactionId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder
            .HasOne(pa => pa.Payment)
            .WithMany(p => p.Allocations)
            .HasForeignKey(pa => pa.PaymentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(pa => pa.Amount)
            .HasCurrencyPrecision()
            .IsRequired();
    }
}
