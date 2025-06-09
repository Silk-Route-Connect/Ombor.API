using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TemplateItemConfiguration : IEntityTypeConfiguration<TemplateItem>
{
    public void Configure(EntityTypeBuilder<TemplateItem> builder)
    {
        builder.ToTable(nameof(TemplateItem));

        builder.HasKey(ti => ti.Id);

        builder
            .HasOne(ti => ti.Product)
            .WithMany(p => p.TemplateItems)
            .HasForeignKey(ti => ti.ProductId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder
            .HasOne(ti => ti.Template)
            .WithMany(t => t.Items)
            .HasForeignKey(ti => ti.TemplateId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        #region Properties

        builder
            .Property(ti => ti.Quantity)
            .IsRequired();

        builder
            .Property(ti => ti.UnitPrice)
            .HasCurrencyPrecision();

        builder
            .Property(ti => ti.DiscountAmount)
            .HasCurrencyPrecision();

        #endregion
    }
}
