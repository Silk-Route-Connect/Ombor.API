using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable(nameof(Payment));

        builder.HasKey(p => p.Id);

        builder
            .HasOne(payment => payment.Partner)
            .WithMany(partner => partner.Payments)
            .HasForeignKey(payment => payment.PartnerId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .HasMany(p => p.Components)
            .WithOne(c => c.Payment)
            .HasForeignKey(c => c.PaymentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasMany(p => p.Allocations)
            .WithOne(a => a.Payment)
            .HasForeignKey(p => p.PaymentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder
            .HasOne(p => p.Employee)
            .WithMany(e => e.Payrolls)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .HasOne(p => p.Wallet)
            .WithMany()
            .HasForeignKey(p => p.WalletId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder
            .Property(p => p.Number)
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired(false);

        // The payment number is the dispute trail's human handle: unique per organization so concurrent
        // creates can't mint the same «P-n» (filtered to skip the nullable column's single-NULL rule).
        builder
            .HasIndex(p => new { p.OrganizationId, p.Number })
            .IsUnique()
            .HasFilter("[Number] IS NOT NULL");

        builder
            .Property(p => p.Notes)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(p => p.Type)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();

        builder
            .Property(p => p.Direction)
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.EnumLength)
            .IsRequired();

        builder
            .Property(p => p.DateUtc)
            .IsRequired();

        builder
            .Property(p => p.Period)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);

        builder
            .Property(p => p.Salary)
            .HasCurrencyPrecision()
            .IsRequired(false);
    }
}
