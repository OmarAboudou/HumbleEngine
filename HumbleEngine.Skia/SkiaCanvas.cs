using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaCanvas : ICanvas
{
    private readonly SKCanvas _canvas;

    internal SkiaCanvas(SKCanvas canvas)
    {
        _canvas = canvas;
    }

    public void Save()    => _canvas.Save();
    public void Restore() => _canvas.Restore();

    public void SetMatrix(Matrix matrix) => _canvas.SetMatrix(ToSk(matrix));
    public void Concat(Matrix matrix)    => _canvas.Concat(ToSk(matrix));

    public void Clear(Color color) => _canvas.Clear(ToSk(color));

    public void DrawRect(Rect rect, IPaint paint)
        => _canvas.DrawRect(ToSk(rect), ToSk(paint));

    public void DrawRoundRect(RoundRect rrect, IPaint paint)
        => _canvas.DrawRoundRect(ToSk(rrect), ToSk(paint));

    public void DrawCircle(float cx, float cy, float radius, IPaint paint)
        => _canvas.DrawCircle(cx, cy, radius, ToSk(paint));

    public void DrawLine(float x0, float y0, float x1, float y1, IPaint paint)
        => _canvas.DrawLine(x0, y0, x1, y1, ToSk(paint));

    public void DrawPath(IPath path, IPaint paint)
        => _canvas.DrawPath(((SkiaPath)path).NativePath, ToSk(paint));

    public void DrawImage(IImage image, float x, float y, IPaint? paint = null)
        => _canvas.DrawImage(((SkiaImage)image).NativeImage, x, y, paint is null ? null : ToSk(paint));

    public float MeasureText(string text, Font font)
    {
        using var skFont = ToSk(font);
        return skFont.MeasureText(text);
    }

    public void DrawText(string text, float x, float y, Font font, IPaint paint)
    {
        using var skFont = ToSk(font);
        _canvas.DrawText(text, x, y, skFont, ToSk(paint));
    }

    // ── Conversions ────────────────────────────────────────────

    internal static SKColor ToSk(Color c) => new(c.R, c.G, c.B, c.A);

    internal static SKRect ToSk(Rect r) => new(r.Left, r.Top, r.Right, r.Bottom);

    internal static SKRoundRect ToSk(RoundRect rr)
        => new(ToSk(rr.Rect), rr.RadiusX, rr.RadiusY);

    internal static SKMatrix ToSk(Matrix m) => new()
    {
        ScaleX = m.ScaleX, SkewX  = m.SkewX,  TransX = m.TransX,
        SkewY  = m.SkewY,  ScaleY = m.ScaleY, TransY = m.TransY,
        Persp0 = m.Persp0, Persp1 = m.Persp1, Persp2 = m.Persp2,
    };

    internal static SKPaint ToSk(IPaint paint)
    {
        var sk = new SKPaint
        {
            Color       = ToSk(paint.Color),
            Style       = ToSk(paint.Style),
            StrokeWidth = paint.StrokeWidth,
            IsAntialias = paint.IsAntialias,
        };
        if (paint.Shader is SkiaShader shader)
            sk.Shader = shader.NativeShader;
        return sk;
    }

    internal static SKPaintStyle ToSk(PaintStyle style) => style switch
    {
        PaintStyle.Fill          => SKPaintStyle.Fill,
        PaintStyle.Stroke        => SKPaintStyle.Stroke,
        PaintStyle.StrokeAndFill => SKPaintStyle.StrokeAndFill,
        _                        => SKPaintStyle.Fill,
    };

    internal static SKFont ToSk(Font font)
    {
        if (font.Typeface.HasValue)
        {
            var tf    = font.Typeface.Value;
            var style = (tf.IsBold, tf.IsItalic) switch
            {
                (true,  true)  => SKFontStyle.BoldItalic,
                (true,  false) => SKFontStyle.Bold,
                (false, true)  => SKFontStyle.Italic,
                _              => SKFontStyle.Normal,
            };
            using var skTypeface = SKTypeface.FromFamilyName(tf.FamilyName, style);
            return new SKFont(skTypeface, font.Size, font.ScaleX, font.SkewX);
        }
        return new SKFont(SKTypeface.Default, font.Size, font.ScaleX, font.SkewX);
    }
}
