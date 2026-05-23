using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaPaint : IPaint
{
    public Color      Color       { get; set; } = Color.Black;
    public PaintStyle Style       { get; set; } = PaintStyle.Fill;
    public float      StrokeWidth { get; set; } = 1f;
    public bool       IsAntialias { get; set; } = true;
    public IShader?   Shader      { get; set; }
}
