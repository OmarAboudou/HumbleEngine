using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaShader : IShader, IDisposable
{
    internal SKShader NativeShader { get; }

    public SkiaShader(SKShader shader)
    {
        NativeShader = shader;
    }

    public static SkiaShader CreateLinearGradient(
        Vector2<float> start, Vector2<float> end,
        Color[] colors, float[]? positions = null)
    {
        var skColors = System.Array.ConvertAll(colors, SkiaCanvas.ToSk);
        var sk = SKShader.CreateLinearGradient(
            new SKPoint(start.X, start.Y),
            new SKPoint(end.X,   end.Y),
            skColors,
            positions,
            SKShaderTileMode.Clamp
        );
        return new SkiaShader(sk);
    }

    public static SkiaShader CreateRadialGradient(
        Vector2<float> center, float radius,
        Color[] colors, float[]? positions = null)
    {
        var skColors = System.Array.ConvertAll(colors, SkiaCanvas.ToSk);
        var sk = SKShader.CreateRadialGradient(
            new SKPoint(center.X, center.Y),
            radius,
            skColors,
            positions,
            SKShaderTileMode.Clamp
        );
        return new SkiaShader(sk);
    }

    public void Dispose() => NativeShader.Dispose();
    public static IShader CreateLinearGradient()
    {
        throw new NotImplementedException();
    }
}
