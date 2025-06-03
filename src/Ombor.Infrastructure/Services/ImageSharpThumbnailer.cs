using Microsoft.Extensions.Options;
using Ombor.Application.Configurations;
using Ombor.Application.Interfaces.File;
using Ombor.Application.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Ombor.Infrastructure.Services;

internal sealed class ImageSharpThumbnailer(IOptions<FileSettings> settings) : IImageThumbnailer
{
    private readonly Size _size = new(settings.Value.ThumbnailWidth, settings.Value.ThumbnailHeight);

    public async Task<Stream> GenerateThumbnailAsync(
        Stream source,
        ThumbnailFormat format,
        CancellationToken cancellationToken = default)
    {
        source.Position = 0;
        using var image = await Image.LoadAsync(source, cancellationToken);
        image.Mutate(x => x.Resize(
            new ResizeOptions
            {
                Size = _size,
                Mode = ResizeMode.Max
            }));

        return await SaveImageAsync(image, format, cancellationToken);
    }

    private static async Task<Stream> SaveImageAsync(Image? image, ThumbnailFormat format, CancellationToken cancellation = default)
    {
        var output = new MemoryStream();

        switch (format)
        {
            case ThumbnailFormat.Png:
                await image.SaveAsPngAsync(output, cancellation);
                break;
            case ThumbnailFormat.Jpg:
                await image.SaveAsJpegAsync(output, cancellation);
                break;
            case ThumbnailFormat.Webp:
                await image.SaveAsWebpAsync(output, cancellation);
                break;
            case ThumbnailFormat.Gif:
                await image.SaveAsGifAsync(output, cancellation);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Saving image in {format} is not supported.");
        }

        output.Position = 0;
        return output;
    }
}
