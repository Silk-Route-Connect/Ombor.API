namespace Ombor.Tests.Unit.Services;

//public sealed class FileServiceTests : IDisposable
//{
//    private readonly string _tempRoot;
//    private readonly FileService _service;

//    public FileServiceTests()
//    {
//        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
//        Directory.CreateDirectory(_tempRoot);

//        var fileSettings = new FileSettings
//        {
//            BasePath = "uploads",
//            MaxBytes = 1024 * 1024,
//            AllowedExtensions = new[] { ".txt", ".jpg" },
//            ThumbnailSize = new Size(100, 100),
//            OriginalsSubfolder = "originals",
//            ThumbnailsSubfolder = "thumbs"
//        };

//        var optionsMock = new Mock<IOptions<FileSettings>>();
//        optionsMock.Setup(o => o.Value).Returns(fileSettings);

//        var envMock = new Mock<IWebHostEnvironment>();
//        envMock.Setup(e => e.WebRootPath).Returns(_tempRoot);

//        _service = new FileService(optionsMock.Object, envMock.Object);
//    }

//    public void Dispose()
//    {
//        if (Directory.Exists(_tempRoot))
//            Directory.Delete(_tempRoot, true);
//    }

//    [Fact]
//    public async Task SaveAsync_ShouldSaveFileAndReturnUrl_WhenValidFileIsProvided()
//    {
//        // Arrange
//        var content = Encoding.UTF8.GetBytes("Test content");
//        var file = CreateFormFile(content, "test.txt");

//        // Act
//        var result = await _service.SaveAsync(file);
//        var files = Directory.GetFiles(_tempRoot);

//        // Assert
//        Assert.Equal("test.txt", result.OriginalFileName);
//        Assert.StartsWith("/uploads/", result.Url);
//        Assert.EndsWith(".txt", result.Url);
//        Assert.Null(result.ThumbnailUrl);

//        var savedPath = Path.Combine(_tempRoot, "uploads", "originals", Path.GetFileName(result.Url));
//        Assert.True(File.Exists(savedPath));
//        Assert.Equal(content, await File.ReadAllBytesAsync(savedPath));
//    }

//    [Fact]
//    public async Task SaveAsync_ShouldReturnThumbnailUrl_WhenImageFileProvided()
//    {
//        // Arrange
//        var imageBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/P3cYdQAAAABJRU5ErkJggg==");
//        var file = CreateFormFile(imageBytes, "photo.jpg");

//        // Act
//        var result = await _service.SaveAsync(file);

//        // Assert
//        Assert.Equal("photo.jpg", result.OriginalFileName);
//        Assert.NotNull(result.ThumbnailUrl);
//        Assert.Contains("/thumbs/", result.ThumbnailUrl);
//    }

//    [Theory]
//    [InlineData(null, 0, ".txt", "No file provided.")]
//    [InlineData("file.txt", 0, ".txt", "No file provided.")]
//    [InlineData("file.txt", 2000000, ".txt", "File size is too large")]
//    [InlineData("file.exe", 100, ".exe", "Invalid file type")]
//    [InlineData("", 100, "", "File name must be provided")]
//    public async Task SaveAsync_ShouldThrow_WhenFileInvalid(string fileName, int length, string extension, string expectedError)
//    {
//        // Arrange
//        IFormFile? file = fileName == null ? null : CreateFormFile(new byte[length], $"{fileName}{extension}");

//        // Act & Assert
//        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SaveAsync(file!));
//        Assert.Contains(expectedError, ex.Message, StringComparison.OrdinalIgnoreCase);
//    }

//    [Theory]
//    [InlineData("..")]
//    [InlineData("images//test")]
//    [InlineData(@"C:\hack")]
//    [InlineData("a/../b")]
//    public async Task SaveAsync_ShouldThrowArgumentException_WhenSubfolderInvalid(string subfolder)
//    {
//        var file = CreateFormFile(new byte[] { 1 }, "valid.txt");

//        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
//            _service.SaveAsync(file, subfolder));

//        Assert.StartsWith("Invalid subfolder path", ex.Message);
//    }

//    [Theory]
//    [InlineData("TEST.TXT")]
//    [InlineData("Test.TxT")]
//    [InlineData("test.txt")]
//    public async Task SaveAsync_ShouldAcceptFile_WhenExtensionMatchesIgnoringCase(string fileName)
//    {
//        // Arrange
//        var content = new byte[] { 10, 20 };
//        var file = CreateFormFile(content, fileName);

//        // Act
//        var result = await _service.SaveAsync(file);

//        // Assert
//        Assert.Equal(fileName, result.OriginalFileName);
//        Assert.EndsWith(".txt", result.Url); // Always normalized
//        var fullPath = Path.Combine(_tempRoot, "uploads", "originals", Path.GetFileName(result.Url));
//        Assert.True(File.Exists(fullPath));
//    }

//    [Fact]
//    public async Task SaveAsync_ShouldReturnEncodedUrl_WhenSubfolderHasSpaces()
//    {
//        var file = CreateFormFile(new byte[] { 1, 2 }, "my file.txt");

//        var result = await _service.SaveAsync(file, "space folder");

//        Assert.Contains("/space%20folder/", result.Url);
//    }

//    private static FormFile CreateFormFile(byte[] content, string fileName)
//    {
//        var stream = new MemoryStream(content);
//        return new FormFile(stream, 0, content.Length, "file", fileName);
//    }
//}
