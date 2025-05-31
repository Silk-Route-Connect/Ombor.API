using System.Text;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Ombor.Infrastructure.Storage;

namespace Ombor.Tests.Unit.Services;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _webRootPath;
    private readonly LocalFileStorage _storage;

    public LocalFileStorageTests()
    {
        _webRootPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_webRootPath);

        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(_webRootPath);

        _storage = new LocalFileStorage(envMock.Object);
    }

    public static TheoryData<string, string[]> ValidPathData => new()
    {
        { "file.txt", new[] { "file.txt" } },
        { "sub/file.txt", new[] { "sub", "file.txt" } },
        { "a/b/c.txt", new[] { "a", "b", "c.txt" } },
        { "mixed\\separators.txt", new[] { "mixed", "separators.txt" } },
        { "/leading/slash.txt", new[] { "leading", "slash.txt" } },
        { "trailing/slash/", new[] { "trailing", "slash" } },
    };

    public static TheoryData<string?, Type> InvalidPathData => new()
    {
        { null, typeof(ArgumentNullException) },
        { "" , typeof(ArgumentException) },
        {"  ", typeof(ArgumentException)},
        { "..", typeof(ArgumentException) },
        { "sub/../file.txt", typeof(ArgumentException) },
        { "inva|id/file.txt", typeof(ArgumentException) }
    };

    [Theory]
    [MemberData(nameof(ValidPathData))]
    public async Task UploadAsync_SavesContentAndReturnsRelativePath_WhenStoragePathIsValid(
        string storagePath,
        string[] expectedSegments)
    {
        // Arrange
        byte[] fileBytes = Encoding.UTF8.GetBytes("Hello, world!");
        await using var contentStream = new MemoryStream(fileBytes);

        // Act
        await _storage.SaveAsync(
            contentStream,
            storagePath,
            CancellationToken.None);

        // Assert
        string physicalPath = Path.Combine(_webRootPath, Path.Combine(expectedSegments));
        Assert.True(File.Exists(physicalPath), "Uploaded file should exist on disk.");

        byte[] writtenBytes = await File.ReadAllBytesAsync(physicalPath);
        Assert.Equal(fileBytes, writtenBytes);
    }

    [Theory]
    [MemberData(nameof(InvalidPathData))]
    public async Task UploadAsync_ShouldThrowArgumentException_WhenStoragePathIsInvalid(
        string storagePath,
        Type exceptionType)
    {
        // Arrange
        await using var contentStream = new MemoryStream([1, 2, 3]);

        // Act & Assert
        await Assert.ThrowsAsync(
            exceptionType,
            async () =>
            {
                await _storage.SaveAsync(
                    contentStream,
                    storagePath,
                    CancellationToken.None);
            });
    }

    [Fact]
    public async Task UploadAsync_ShouldThrowArgumentNullException_WhenStreamIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await _storage.SaveAsync(
                null!,
                Guid.NewGuid().ToString(),
                CancellationToken.None);
        });
    }

    [Theory]
    [MemberData(nameof(ValidPathData))]
    public async Task DeleteAsync_ShouldRemoveFile_WhenFileExists(
            string storagePath,
            string[] expectedSegments)
    {
        // Arrange
        string physicalPath = Path.Combine(_webRootPath, Path.Combine(expectedSegments));
        string directory = Path.GetDirectoryName(physicalPath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllBytesAsync(physicalPath, Encoding.UTF8.GetBytes("to be deleted"));

        Assert.True(File.Exists(physicalPath), "Precondition failed: file should exist before DeleteAsync.");

        // Act
        await _storage.DeleteAsync(storagePath, CancellationToken.None);

        // Assert
        Assert.False(File.Exists(physicalPath), "DeleteAsync should have removed the file.");
    }

    [Theory]
    [MemberData(nameof(ValidPathData))]
    public async Task DeleteAsync_ShouldNotThrow_WhenFileDoesNotExist(
        string storagePath,
        string[] expectedSegments)
    {
        // Arrange
        string physicalPath = Path.Combine(_webRootPath, Path.Combine(expectedSegments));
        if (File.Exists(physicalPath))
        {
            File.Delete(physicalPath);
        }

        // Act & Assert
        await _storage.DeleteAsync(storagePath, CancellationToken.None);

        // Still no file
        Assert.False(File.Exists(physicalPath), "There should be no file to delete.");
    }

    [Theory]
    [MemberData(nameof(InvalidPathData))]
    public async Task DeleteAsync_ShouldThrow_WhenStoragePathIsInvalid(
        string storagePath,
        System.Type exceptionType)
    {
        await Assert.ThrowsAsync(
            exceptionType,
            async () => await _storage.DeleteAsync(storagePath, CancellationToken.None));
    }

    public void Dispose()
    {
        if (Directory.Exists(_webRootPath))
        {
            Directory.Delete(_webRootPath, recursive: true);
        }
    }
}