﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class TemplateConfiguration : IEntityTypeConfiguration<Template>
{
    public void Configure(EntityTypeBuilder<Template> builder)
    {
        builder.ToTable(nameof(Template));

        builder.HasKey(t => t.Id);

        builder
            .HasMany(t => t.Items)
            .WithOne(ti => ti.Template)
            .HasForeignKey(ti => ti.TemplateId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(t => t.Partner)
            .WithMany(p => p.Templates)
            .HasForeignKey(t => t.PartnerId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .Navigation(t => t.Items)
            .AutoInclude();

        #region Properties

        builder
            .Property(t => t.Name)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(t => t.Type)
            .HasConversion<string>()
            .IsRequired();

        #endregion
    }
}
