using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ombor.Infrastructure.Persistence.Configurations;

namespace Ombor.Infrastructure.Extensions;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder<decimal> HasCurrencyPrecision(this PropertyBuilder<decimal> builder)
        => builder.HasPrecision(18, 2);

    public static PropertyBuilder<decimal> HasQuantityPrecision(this PropertyBuilder<decimal> builder)
        => builder.HasPrecision(18, 3);

    internal static PropertyBuilder<T> HasEnumConversion<T>(this PropertyBuilder<T> builder) where T : Enum
        => builder
        .HasConversion<string>()
        .HasMaxLength(ConfigurationConstants.EnumConversionLength);
}
