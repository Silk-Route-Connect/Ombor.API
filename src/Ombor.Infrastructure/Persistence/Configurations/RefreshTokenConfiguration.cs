using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable(nameof(RefreshToken));

        builder.HasKey(rt => rt.Id);

        builder
            .HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(rt => rt.Token)
            .IsUnique();

        builder.
            Property(rt => rt.Token)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired();

        builder
            .Property(rt => rt.ExpiresAt)
            .IsRequired();

        builder
            .Property(rt => rt.IsRevoked)
            .HasDefaultValue(false)
            .IsRequired();
    }
}
