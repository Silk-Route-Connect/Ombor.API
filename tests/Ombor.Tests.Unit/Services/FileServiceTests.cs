using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Models;
using Ombor.Application.Services;
using Ombor.Domain.Exceptions;

namespace Ombor.Tests.Unit.Services;

public sealed class FileServiceTests
{
    private readonly Mock<IFileStorage> _storageMock;
    private readonly Mock<IImageThumbnailer> _thumbnailerMock;
    private readonly Mock<IFilePathProvider> _pathResolverMock;
    private readonly FileSettings _settings;
    private readonly FileService _service;

    public FileServiceTests()
    {
        _storageMock = new Mock<IFileStorage>(MockBehavior.Strict);
        _thumbnailerMock = new Mock<IImageThumbnailer>(MockBehavior.Strict);
        _pathResolverMock = new Mock<IFilePathProvider>(MockBehavior.Strict);

        _settings = new()
        {
            BasePath = "uploads",
            MaxBytes = 200,
            AllowedFileExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif", ".pdf"],
            AllowedImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".gif"],
            ThumbnailWidth = 100,
            ThumbnailHeight = 100,
            OriginalsSubfolder = "originals",
            ThumbnailsSubfolder = "thumbnails",
            ProductUploadsSection = "products",
            PublicUrlPrefix = "images"
        };
        var options = Options.Create(_settings);

        _service = new FileService(
            _storageMock.Object,
            _thumbnailerMock.Object,
            _pathResolverMock.Object,
            options,
            new NullLogger<FileService>());
    }

    public static TheoryData<byte[]?, string?, Type> InvalidFileData => new()
    {
        { null, "file.png", typeof(ArgumentException) },
        { Array.Empty<byte>(), "file.png", typeof(ArgumentException) },
        { new byte[10], null, typeof(ArgumentException) },
        { new byte[10], "", typeof(ArgumentException) },
        { new byte[10], "  ", typeof(ArgumentException) },
        { new byte[250], "file.png", typeof(FileTooLargeException) },
        { new byte[10], "file.exe", typeof(UnsupportedFileFormatException) }
    };

    [Theory]
    [MemberData(nameof(InvalidFileData))]
    public async Task UploadAsync_ShouldThrow_WhenFileIsInvalid(byte[]? content, string? fileName, Type exceptionType)
    {
        // Arrange
        IFormFile file = CreateFormFile(content, fileName);

        // Act & Assert
        await Assert.ThrowsAsync(
            exceptionType,
            () => _service.UploadAsync(file, _settings.ProductUploadsSection, CancellationToken.None));
    }

    [Theory]
    [InlineData(".png", ThumbnailFormat.Png)]
    [InlineData(".jpg", ThumbnailFormat.Jpg)]
    [InlineData(".jpeg", ThumbnailFormat.Jpeg)]
    [InlineData(".webp", ThumbnailFormat.Webp)]
    [InlineData(".gif", ThumbnailFormat.Gif)]
    public async Task UpdateAsync_ShouldSaveThumbnailWithCorrectExtensionFormat_WhenFileIsImage(string extension, ThumbnailFormat expectedFormat)
    {
        // Arrange
        byte[] data = [1, 2, 3];
        IFormFile file = CreateFormFile(data, $"test{extension}");

        string generated = Guid.NewGuid().ToString("N") + ".jpg";
        string originalImagePath = $"uploads/images/{_settings.ProductUploadsSection}/originals/{generated}";
        string thumbnailImagePath = $"uploads/images/{_settings.ProductUploadsSection}/thumbnails/{generated}";
        string originalImageUrl = $"/{originalImagePath}";
        string thumbnailImageUrl = $"/{thumbnailImagePath}";

        _pathResolverMock.Setup(x => x.BuildRelativePath(_settings.ProductUploadsSection, _settings.OriginalsSubfolder, It.IsAny<string>()))
            .Returns(originalImagePath);
        _pathResolverMock.Setup(x => x.BuildRelativePath(_settings.ProductUploadsSection, _settings.ThumbnailsSubfolder, It.IsAny<string>()))
            .Returns(thumbnailImagePath);

        _storageMock.Setup(x => x.SaveAsync(It.IsAny<Stream>(), originalImagePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageMock.Setup(x => x.SaveAsync(It.IsAny<Stream>(), thumbnailImagePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _pathResolverMock.Setup(x => x.BuildPublicUrl(_settings.ProductUploadsSection, _settings.OriginalsSubfolder, It.IsAny<string>()))
            .Returns(originalImageUrl);
        _pathResolverMock.Setup(x => x.BuildPublicUrl(_settings.ProductUploadsSection, _settings.ThumbnailsSubfolder, It.IsAny<string>()))
            .Returns(thumbnailImageUrl);

        var thumbStream = new MemoryStream([9, 8, 7]);
        _thumbnailerMock.Setup(x => x.GenerateThumbnailAsync(It.IsAny<Stream>(), expectedFormat, It.IsAny<CancellationToken>()))
            .ReturnsAsync(thumbStream);

        // Act
        var result = await _service.UploadAsync(file, _settings.ProductUploadsSection, CancellationToken.None);

        // Assert
        Assert.EndsWith(extension, result.FileName);
        Assert.Equal($"test{extension}", result.OriginalFileName);
        Assert.Equal(originalImageUrl, result.Url);
        Assert.Equal(thumbnailImageUrl, result.ThumbnailUrl);
        _pathResolverMock.VerifyAll();
        _storageMock
            .VerifyAll();
        _thumbnailerMock.VerifyAll();
    }

    [Fact]
    public async Task UploadAsync_ShouldSaveOriginalOnly_WhenFileIsNotImage()
    {
        // Arrange
        byte[] data = [1, 2, 3];
        IFormFile file = CreateFormFile(data, "document.pdf");
        string generated = Guid.NewGuid().ToString("N") + ".pdf";
        string originalPath = $"uploads/documents/originals/{generated}";
        string originalUrl = $"/{originalPath}";

        _pathResolverMock.Setup(x => x.BuildRelativePath("documents", "originals", It.IsAny<string>()))
            .Returns(originalPath);

        _storageMock.Setup(x => x.SaveAsync(It.IsAny<Stream>(), originalPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _pathResolverMock.Setup(x => x.BuildPublicUrl("documents", "originals", It.IsAny<string>()))
            .Returns(originalUrl);

        // Act
        var result = await _service.UploadAsync(file, "documents", CancellationToken.None);

        // Assert
        Assert.EndsWith(".pdf", result.FileName);
        Assert.Equal("document.pdf", result.OriginalFileName);
        Assert.Equal(originalUrl, result.Url);
        Assert.Null(result.ThumbnailUrl);
    }

    [Fact]
    public async Task UploadAsync_ShouldSwallowThumbnailException_WhenThumbnailerFails()
    {
        // Arrange
        byte[] data = [1, 2, 3];
        IFormFile file = CreateFormFile(data, "image.png");
        string generated = Guid.NewGuid().ToString("N") + ".png";
        string originalPath = $"uploads/images/originals/{generated}";
        string originalUrl = $"/{originalPath}";

        _pathResolverMock.Setup(x => x.BuildRelativePath("images", "originals", It.IsAny<string>()))
            .Returns(originalPath);

        _storageMock.Setup(x => x.SaveAsync(It.IsAny<Stream>(), originalPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _pathResolverMock.Setup(x => x.BuildPublicUrl("images", "originals", It.IsAny<string>()))
            .Returns(originalUrl);

        _thumbnailerMock.Setup(x => x.GenerateThumbnailAsync(It.IsAny<Stream>(), ThumbnailFormat.Png, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Thumbnail generation failed"));

        // Act
        var result = await _service.UploadAsync(file, "images", CancellationToken.None);

        // Assert
        Assert.EndsWith(".png", result.FileName);
        Assert.Equal("image.png", result.OriginalFileName);
        Assert.Equal(originalUrl, result.Url);
        Assert.Null(result.ThumbnailUrl);
    }

    [Theory]
    [InlineData(null, typeof(ArgumentNullException))]
    [InlineData("", typeof(ArgumentException))]
    [InlineData("  ", typeof(ArgumentException))]
    public async Task DeleteAsync_ShouldThrowArgumentException_WhenFileNameIsInvalid(string fileName, Type exception)
    {
        // Act & Assert
        await Assert.ThrowsAsync(
            exception,
            () => _service.DeleteAsync(fileName, "images", CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallStorageDeleteTwice_WhenFileIsImage()
    {
        // Arrange
        string fileName = "test.png";
        string subfolder = "images";
        string relativePath = $"uploads/{subfolder}/originals/{fileName}";
        string thumbnailPath = $"uploads/{subfolder}/thumbnails/{fileName}";

        _pathResolverMock.Setup(x => x.BuildRelativePath(subfolder, "originals", fileName))
            .Returns(relativePath);
        _storageMock.Setup(x => x.DeleteAsync(relativePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _pathResolverMock.Setup(x => x.BuildRelativePath(subfolder, "thumbnails", fileName))
            .Returns(thumbnailPath);
        _storageMock.Setup(x => x.DeleteAsync(thumbnailPath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(fileName, subfolder, CancellationToken.None);

        // Assert
        _storageMock.Verify(x => x.DeleteAsync(relativePath, It.IsAny<CancellationToken>()), Times.Once);
        _storageMock.Verify(x => x.DeleteAsync(thumbnailPath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallStorageDeleteOnce_WhenFileIsNotImage()
    {
        // Arrange
        string fileName = "document.pdf";
        string subfolder = "documents";
        string relativePath = $"uploads/{subfolder}/originals/{fileName}";

        _pathResolverMock.Setup(x => x.BuildRelativePath(subfolder, "originals", fileName))
            .Returns(relativePath);
        _storageMock.Setup(x => x.DeleteAsync(relativePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.DeleteAsync(fileName, subfolder, CancellationToken.None);

        // Assert
        _storageMock.Verify(x => x.DeleteAsync(relativePath, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowFileNotFoundException_WhenFileDoesNotExist()
    {
        // Arrange
        string fileName = "nonexistent.png";
        string subfolder = "images";
        string relativePath = $"uploads/{subfolder}/originals/{fileName}";

        _pathResolverMock.Setup(x => x.BuildRelativePath(subfolder, "originals", fileName))
            .Returns(relativePath);
        _storageMock.Setup(x => x.DeleteAsync(relativePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FileNotFoundException("File not found"));

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _service.DeleteAsync(fileName, subfolder, CancellationToken.None));
    }

    private static FormFile CreateFormFile(byte[]? data, string? fileName)
    {
        if (data is null)
        {
            return null!;
        }

        var stream = new MemoryStream(data);
        return new FormFile(stream, 0, stream.Length, "file", fileName!);
    }
}