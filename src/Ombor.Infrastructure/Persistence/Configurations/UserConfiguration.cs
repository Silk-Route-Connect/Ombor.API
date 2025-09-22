using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(nameof(User));

        builder.HasKey(u => u.Id);

        builder.HasOne(u => u.Organization)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Roles)
            .WithMany(ur => ur.Users);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();

        builder
            .HasIndex(u => u.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        builder
            .HasIndex(u => u.TelegramAccount)
            .IsUnique()
            .HasFilter("[TelegramAccount] IS NOT NULL");

        builder
            .Property(u => u.FirstName)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(u => u.LastName)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(u => u.TelegramAccount)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);

        builder
            .Property(u => u.PhoneNumber)
            .HasMaxLength(ConfigurationConstants.PhoneNumberLength)
            .IsRequired();

        builder
            .Property(u => u.Email)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);
    }
}
