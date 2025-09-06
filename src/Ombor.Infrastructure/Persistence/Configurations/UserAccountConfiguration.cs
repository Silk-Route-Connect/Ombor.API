using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable(nameof(UserAccount));

        builder.HasKey(u => u.Id);

        builder
            .HasIndex(u => u.PhoneNumber)
            .IsUnique();

        builder
            .HasIndex(u => u.Email)
            .IsUnique();

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
            .IsRequired();

        builder
            .Property(u => u.PhoneNumber)
            .HasMaxLength(ConfigurationConstants.PhoneNumberLength)
            .IsRequired();

        builder
            .Property(u => u.Email)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
             .Property(u => u.Access)
             .HasConversion<string>()
             .HasMaxLength(ConfigurationConstants.EnumLength)
             .IsRequired();
    }
}
