using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaFont : IFont, IDisposable
{
    private readonly SKFont _font;

    public ITypeface Typeface { get; }
    public float     Size     => _font.Size;
    public float     ScaleX   => _font.ScaleX;
    public float     SkewX    => _font.SkewX;

    public SkiaFont(SkiaTypeface typeface, float size, float scaleX = 1f, float skewX = 0f)
    {
        Typeface = typeface;
        _font    = new SKFont(typeface.NativeTypeface, size, scaleX, skewX);
    }

    public float MeasureText(string text) => _font.MeasureText(text);

    public void Dispose() => _font.Dispose();
}
