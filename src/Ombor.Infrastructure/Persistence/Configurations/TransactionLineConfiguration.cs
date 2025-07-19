using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TransactionLineConfiguration : IEntityTypeConfiguration<TransactionLine>
{
    public void Configure(EntityTypeBuilder<TransactionLine> builder)
    {
        builder.ToTable(nameof(TransactionLine));

        builder.HasKey(tl => tl.Id);

        builder
            .HasOne(tl => tl.Transaction)
            .WithMany(t => t.Lines)
            .HasForeignKey(tl => tl.TransactionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(tl => tl.Product)
            .WithMany(p => p.Lines)
            .HasForeignKey(tl => tl.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .Property(tl => tl.UnitPrice)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(tl => tl.Discount)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(tl => tl.Quantity)
            .HasCurrencyPrecision()
            .IsRequired();
    }
}
