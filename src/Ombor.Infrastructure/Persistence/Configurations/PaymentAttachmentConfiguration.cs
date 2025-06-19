using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class PaymentAttachmentConfiguration : IEntityTypeConfiguration<PaymentAttachment>
{
    public void Configure(EntityTypeBuilder<PaymentAttachment> builder)
    {
        builder.ToTable(nameof(PaymentAttachment));

        builder.HasKey(pa => pa.Id);

        builder
            .HasOne(pa => pa.Payment)
            .WithMany(p => p.Attachments)
            .HasForeignKey(pa => pa.PaymentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        #region

        builder
            .Property(pa => pa.FileName)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(pa => pa.FileUrl)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(pa => pa.Description)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        #endregion
    }
}
