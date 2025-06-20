using Ombor.Application.Configurations;

namespace Ombor.Tests.Common.Factories;

public static class FileSettingsFactory
{
    public static FileSettings CreateDefault() =>
        new()
        {
            BasePath = "uploads",
            MaxBytes = 1000,
            AllowedFileExtensions = [".png", ".jpg", ".pdf"],
            AllowedImageExtensions = [".png", ".jpg"],
            ThumbnailWidth = 100,
            ThumbnailHeight = 100,
            OriginalsSubfolder = "originals",
            ThumbnailsSubfolder = "thumbnails",
            ProductUploadsSection = "products",
            PublicUrlPrefix = "images",
            PaymentAttachmentsSection = "payments"
        };
}
