namespace Ombor.Tests.Common.Helpers;

/// <summary>
/// Minimal upload bodies with the real leading bytes (magic numbers) of each type. Uploads are validated by content,
/// so a test file must start like the type its extension claims.
/// </summary>
public static class TestFiles
{
    public static byte[] Jpeg => [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    public static byte[] Png => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    public static byte[] Pdf => [.. "%PDF-1.7\n%âã"u8];
}
