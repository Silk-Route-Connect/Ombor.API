using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ombor.Infrastructure.Extensions;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder<decimal> HasCurrencyPrecision(this PropertyBuilder<decimal> builder)
        => builder.HasPrecision(18, 2);
}
