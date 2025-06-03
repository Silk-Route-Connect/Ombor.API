namespace Ombor.TestDataGenerator.Helpers;

public static class ImageProvider
{
    private const string ImageFolder = "Resources/Images";

    public static Stream GetImageStream(string fileName)
    {
        var fullPath = Path.Combine(ImageFolder, fileName);
        var fileStream = File.OpenRead(fullPath);
        fileStream.Position = 0;

        return fileStream;
    }
}
