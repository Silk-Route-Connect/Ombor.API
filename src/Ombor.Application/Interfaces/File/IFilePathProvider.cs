namespace Ombor.Application.Interfaces.File;

/// <summary>
/// Builds storage-relative paths and public URLs for uploaded files.
/// </summary>
public interface IFilePathProvider
{
    /// <summary>
    /// Returns a storage-relative path, e.g. "products/sales/section/filename.ext"
    /// </summary>
    string BuildRelativePath(string? subfolder, string section, string fileName);
}
