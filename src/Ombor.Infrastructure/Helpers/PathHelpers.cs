using System.Runtime.CompilerServices;
using Ombor.Infrastructure.Extensions;

namespace Ombor.Infrastructure.Helpers;

internal static class PathHelpers
{
    private static readonly char[] _invalidPathAndFileNameChars = [.. Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).Distinct()];

    internal static void ValidateSegments(IEnumerable<string> segments, [CallerArgumentExpression(nameof(segments))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(segments);

        foreach (var segment in segments)
        {
            Console.WriteLine($"Invalid path chars: {string.Join(", ", _invalidPathAndFileNameChars)}");
            if (segment == ".." || segment.ContainsAny(_invalidPathAndFileNameChars))
            {
                throw new ArgumentException($"Invalid path segment: {segment}", paramName);
            }
        }
    }

    internal static string[] ExtractSegments(string path, [CallerArgumentExpression(nameof(path))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(path, paramName);

        var segments = path
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

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
