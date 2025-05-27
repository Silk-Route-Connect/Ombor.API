using global::Ombor.Application.Configurations;
using global::Ombor.Application.Interfaces.File;
using global::Ombor.Application.Models;
using global::Ombor.Application.Services;
using global::Ombor.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Ombor.Tests.Unit.Services;

public sealed class FileUploadServiceTests
{
    private readonly Mock<IFileStorage> _storageMock;
    private readonly Mock<IImageThumbnailer> _thumbnailerMock;
    private readonly Mock<IFilePathProvider> _pathResolverMock;
    private readonly FileSettings _settings;
    private readonly FileUploadService _service;

    public FileUploadServiceTests()
    {
        _storageMock = new Mock<IFileStorage>(MockBehavior.Strict);
        _thumbnailerMock = new Mock<IImageThumbnailer>(MockBehavior.Strict);
        _pathResolverMock = new Mock<IFilePathProvider>(MockBehavior.Strict);

        _settings = new FileSettings
        {
            BasePath = "uploads",
            MaxBytes = 1024,
            AllowedFileExtensions = [".png", ".jpg"],
            AllowedImageExtensions = [".png"],
            ThumbnailWidth = 50,
            ThumbnailHeight = 50,
            OriginalsSubfolder = "orig",
            ThumbnailsSubfolder = "thumb"
        };

        var options = Options.Create(_settings);
        _service = new FileUploadService(
            _storageMock.Object,
            _thumbnailerMock.Object,
            _pathResolverMock.Object,
            options,
            new NullLogger<FileUploadService>());
    }

    public static IEnumerable<object?[]> InvalidFileData()
    {
        yield return new object?[] { null, "file.png", typeof(ArgumentException) };
        yield return new object?[] { Array.Empty<byte>(), "file.png", typeof(ArgumentException) };
        yield return new object?[] { new byte[10], null, typeof(ArgumentException) };
        yield return new object?[] { new byte[10], "", typeof(ArgumentException) };
        yield return new object?[] { new byte[10], "  ", typeof(ArgumentException) };
        yield return new object?[] { new byte[2000], "file.png", typeof(FileTooLargeException) };
        yield return new object?[] { new byte[10], "file.exe", typeof(UnsupportedFileFormatException) };
    }

    [Theory]
    [MemberData(nameof(InvalidFileData))]
    public async Task UploadAsync_ShouldThrow_WhenFileIsInvalid(
        byte[]? content,
        string? fileName,
        Type exceptionType)
    {
        // Arrange
        IFormFile file = CreateFormFile(content!, fileName);

        // Act & Assert
        await Assert.ThrowsAsync(
            exceptionType,
            () => _service.UploadAsync(file, "subfolder", CancellationToken.None));
    }

    [Fact]
    public async Task UploadAsync_ShouldSaveOriginalAndThumbnail_WhenFileIsImage()
    {
        byte[] data = { 1, 2, 3 };
        string fileName = "pic.png";
        string subfolder = "products";
        IFormFile file = CreateFormFile(data, fileName);

        string generated = Guid.NewGuid().ToString("N") + ".png";
        string origPath = $"uploads/{subfolder}/orig/{generated}";
        string thumbPath = $"uploads/{subfolder}/thumb/{generated}";
        string origUrl = "/" + origPath;
        string thumbUrl = "/" + thumbPath;

        _pathResolverMock
            .Setup(x => x.BuildRelativePath(subfolder, "orig", It.IsAny<string>()))
            .Returns(origPath);
        _pathResolverMock
            .Setup(x => x.BuildRelativePath(subfolder, "thumb", It.IsAny<string>()))
            .Returns(thumbPath);

        _storageMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), origPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(origUrl);
        _storageMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), thumbPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(thumbUrl);

        var thumbStream = new MemoryStream(new byte[] { 9, 8, 7 });
        _thumbnailerMock
            .Setup(x => x.GenerateThumbnailAsync(It.IsAny<Stream>(), ThumbnailFormat.Png, It.IsAny<CancellationToken>()))
            .ReturnsAsync(thumbStream);

        FileUploadResult result = await _service.UploadAsync(file, subfolder, CancellationToken.None);

        Assert.Equal(fileName, result.OriginalFileName);
        Assert.Equal(origUrl, result.Url);
        Assert.Equal(thumbUrl, result.ThumbnailUrl);

        _pathResolverMock.VerifyAll();
        _storageMock.VerifyAll();
        _thumbnailerMock.VerifyAll();
    }

    [Fact]
    public async Task UploadAsync_ShouldSaveOriginalOnly_WhenFileIsNotImage()
    {
        byte[] data = { 1, 2, 3 };
        string fileName = "doc.jpg";
        string subfolder = "reports";
        IFormFile file = CreateFormFile(data, fileName);

        string generated = Guid.NewGuid().ToString("N") + ".jpg";
        string origPath = $"uploads/{subfolder}/orig/{generated}";
        string origUrl = "/" + origPath;

        _pathResolverMock
            .Setup(x => x.BuildRelativePath(subfolder, "orig", It.IsAny<string>()))
            .Returns(origPath);
        _storageMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), origPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(origUrl);

        FileUploadResult result = await _service.UploadAsync(file, subfolder, CancellationToken.None);

        Assert.Equal(origUrl, result.Url);
        Assert.Null(result.ThumbnailUrl);
        _thumbnailerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UploadAsync_ShouldSwallowThumbnailException_WhenThumbnailerFails()
    {
        byte[] data = { 1, 2, 3 };
        string fileName = "pic.png";
        string subfolder = "products";
        IFormFile file = CreateFormFile(data, fileName);

        string generated = Guid.NewGuid().ToString("N") + ".png";
        string origPath = $"uploads/{subfolder}/orig/{generated}";
        string origUrl = "/" + origPath;
        string thumbPath = $"uploads/{subfolder}/thumb/{generated}";

        _pathResolverMock
            .Setup(x => x.BuildRelativePath(subfolder, "orig", It.IsAny<string>()))
            .Returns(origPath);
        _storageMock
            .Setup(x => x.UploadAsync(It.IsAny<Stream>(), origPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(origUrl);
        _thumbnailerMock
            .Setup(x => x.GenerateThumbnailAsync(It.IsAny<Stream>(), ThumbnailFormat.Png, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        _pathResolverMock
            .Setup(x => x.BuildRelativePath(subfolder, "thumb", It.IsAny<string>()))
            .Returns(thumbPath);

        FileUploadResult result = await _service.UploadAsync(file, subfolder, CancellationToken.None);

        Assert.Equal(origUrl, result.Url);
        Assert.Null(result.ThumbnailUrl);
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