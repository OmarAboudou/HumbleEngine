using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaImage : IImage, IDisposable
{
    internal SKImage NativeImage { get; }

    public int Width  => NativeImage.Width;
    public int Height => NativeImage.Height;

    public SkiaImage(SKImage image)
    {
        NativeImage = image;
    }

    public static SkiaImage FromRawImage(RawImage raw)
    {
        var info   = new SKImageInfo(raw.Width, raw.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var bitmap = new SKBitmap(info);
        raw.Pixels.Span.CopyTo(bitmap.GetPixelSpan());
        return new SkiaImage(SKImage.FromBitmap(bitmap));
    }

    public void Dispose() => NativeImage.Dispose();
}
