using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Domain.Entities;

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
            .HasConversion<string>()
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired();

        builder
            .Property(e => e.Salary)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder
            .Property(e => e.Description)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(e => e.Address)
            .HasMaxLength(ConfigurationConstants.MaxStringLength)
            .IsRequired(false);

        builder
            .Property(e => e.Email)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired(false);

        builder
            .Property(e => e.PhoneNumber)
            .HasMaxLength(ConfigurationConstants.DefaultStringLength)
            .IsRequired();

        builder
            .Property(e => e.DateOfEmployment)
            .IsRequired();
    }
}
