using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TransferLineConfiguration : IEntityTypeConfiguration<TransferLine>
{
    public void Configure(EntityTypeBuilder<TransferLine> builder)
    {
        builder.ToTable(nameof(TransferLine));

        builder.HasKey(l => l.Id);

        builder
            .HasOne(l => l.Product)
            .WithMany()
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Same precision as the stock it moves (WarehouseItem), so a fractional transfer is recorded exactly.
        builder
            .Property(l => l.Quantity)
            .HasQuantityPrecision()
            .IsRequired();
    }
}
