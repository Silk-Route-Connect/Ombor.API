using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TransactionAttachmentConfiguration : IEntityTypeConfiguration<TransactionAttachment>
{
    public void Configure(EntityTypeBuilder<TransactionAttachment> builder)
    {
        builder.ToTable(nameof(TransactionAttachment));

        builder.HasKey(ta => ta.Id);

        builder
            .HasOne(ta => ta.Transaction)
            .WithMany(t => t.Attachments)
            .HasForeignKey(ta => ta.TransactionId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Property(ta => ta.FileId)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(ta => ta.FileName)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired();

        builder
            .Property(ta => ta.ContentType)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(ta => ta.Url)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired();
    }
}
