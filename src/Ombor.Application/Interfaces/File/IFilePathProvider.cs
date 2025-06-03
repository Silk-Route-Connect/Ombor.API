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

    /// <summary>
    /// Returns a storage-relative path for the original file, e.g. "images/products/originals/filename.ext"
    /// </summary>
    /// <param name="fileName">Filename</param>
    /// <returns>Public url.</returns>
    string BuildPublicUrl(string? subfolder, string section, string fileName);
}
