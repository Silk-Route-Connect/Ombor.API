using System.Runtime.CompilerServices;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Helpers;

internal static class PathHelpers
{
    private static readonly char[] _invalidPathAndFileNameChars = [.. Path.GetInvalidPathChars(), .. Path.GetInvalidFileNameChars()];

    public static void ValidateSegments(this IEnumerable<string> segments, [CallerArgumentExpression(nameof(segments))] string? paramName = null)
    {
        foreach (var segment in segments)
        {
            if (segment == ".." || segment.ContainsAny(_invalidPathAndFileNameChars))
            {
                throw new ArgumentException($"Invalid path segment: {segment}", paramName);
            }
        }
    }

    internal static string[] ExtractSegments(string path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);

        var segments = path
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        ValidateSegments(segments, paramName);

        return segments;
    }

    internal static string[] BuildSegments(params string?[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var segments = new List<string>();

        foreach (var path in paths)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                segments.Add(path.Trim('/', ' '));
            }
        }

        ValidateSegments(segments);

        return [.. segments];
    }

    internal static string BuildRelativePath(string[] segments)
        => $"/{string.Join("/", segments)}";
}
