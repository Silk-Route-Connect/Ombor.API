using Ombor.Application.Validators;

namespace Ombor.Infrastructure.Persistence.Configurations;

internal static class ConfigurationConstants
{
    public const int DefaultStringLength = ValidationConstants.DefaultStringLength;
    public const int MaxStringLength = ValidationConstants.MaxStringLength;
    public const int EnumLength = 50;
    public const string VarcharMax = "nvarchar(max)";
}
