using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable(nameof(Transfer));

        builder.HasKey(t => t.Id);

        builder
            .HasOne(t => t.FromWarehouse)
            .WithMany()
            .HasForeignKey(t => t.FromWarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasOne(t => t.ToWarehouse)
            .WithMany()
            .HasForeignKey(t => t.ToWarehouseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasMany(t => t.Lines)
            .WithOne(l => l.Transfer)
            .HasForeignKey(l => l.TransferId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(t => t.Notes)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(t => t.CreatedBy)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);

        builder
            .Property(t => t.DateUtc)
            .IsRequired();
    }
}
