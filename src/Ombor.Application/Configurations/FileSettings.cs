using System.ComponentModel.DataAnnotations;

namespace Ombor.Application.Configurations;

public sealed class FileSettings
{
    public const string SectionName = nameof(FileSettings);
    public const string ProductFileUploadsFolder = "products";

    /// <summary>
    /// Maximum allowed file size in bytes
    /// </summary>
    [Required]
    public required long MaxBytes { get; set; }

    /// <summary>
    /// Whitelist of permitted image extensions (include the dot)
    /// </summary>
    [Required]
    public required string[] AllowedImageExtensions { get; init; }

    /// <summary>
    /// Whitelist of permitted file extensions (include the dot)
    /// </summary>
    public required string[] AllowedFileExtensions { get; set; }

    /// <summary>
    /// Physical & URL base folder under wwwroot
    /// </summary>
    [Required(ErrorMessage = "Base path is required for file settings.")]
    [MinLength(3, ErrorMessage = "Base path must have at least 3 characters.")]
    [MaxLength(10, ErrorMessage = "Base path must have maximum of 10 characters.")]
    [RegularExpression(@"^[^\\/:\*\?""<>\|]+$", ErrorMessage = "BasePath must not contain path separators or invalid characters.")]
    public required string BasePath { get; init; }

    [Required(ErrorMessage = "Thumbnail width is required.")]
    public required int ThumbnailWidth { get; set; }

    [Required(ErrorMessage = "Thumbnail height is required.")]
    public required int ThumbnailHeight { get; set; }

    /// <summary>
    /// Physical path for originals subfolder.
    /// </summary>
    [Required(ErrorMessage = "Subfolder for original images is required.")]
    public required string OriginalsSubfolder { get; init; }

    /// <summary>
    /// Physical path for thumbnails subfolder.
    /// </summary>
    [Required(ErrorMessage = "Subfolder for thumbnail images is required.")]
    public required string ThumbnailsSubfolder { get; init; }
}
