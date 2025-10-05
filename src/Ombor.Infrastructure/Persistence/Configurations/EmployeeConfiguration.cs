using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable(nameof(Employee));

        builder.HasKey(e => e.Id);

        builder
            .Property(e => e.FullName)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(e => e.Position)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(e => e.Salary)
            .HasCurrencyPrecision()
            .IsRequired();

        builder
            .Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired();

        builder
            .Property(e => e.DateOfEmployment)
            .IsRequired();

        builder
            .OwnsOne(e => e.ContactInfo, ci =>
            {
                ci.Property(i => i.Email)
                    .HasMaxLength(ConfigurationConstants.DefaultStringLength)
                    .IsRequired(false);

                ci.Property(i => i.Address)
                    .HasMaxLength(ConfigurationConstants.MaxStringLength)
                    .IsRequired(false);

                ci.Property(i => i.TelegramAccount)
                    .HasMaxLength(ConfigurationConstants.DefaultStringLength)
                    .IsRequired(false);

                ci.PrimitiveCollection(i => i.PhoneNumbers)
                    .HasMaxLength(ConfigurationConstants.MaxStringLength)
                    .IsRequired();
            });
    }
}
