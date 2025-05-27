using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces.File;
using Ombor.Infrastructure.Helpers;

namespace Ombor.Infrastructure.Services;

internal sealed class LocalFilePathProvider(IOptions<FileSettings> settings) : IFilePathProvider
{
    private readonly FileSettings _settings = settings.Value;

    public string BuildRelativePath(string? subfolder, string section, string fileName)
    {
        var segments = new[] { _settings.BasePath }
            .Concat(PathHelpers.BuildSegments(subfolder, section, fileName))
            .ToArray();

        return Path.Combine(segments);
    }
}
