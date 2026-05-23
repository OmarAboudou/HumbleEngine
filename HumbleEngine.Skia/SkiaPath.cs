using SkiaSharp;

namespace HumbleEngine.Skia;

public class SkiaPath : IPath, IDisposable
{
    internal SKPath NativePath { get; } = new();

    public void MoveTo(float x, float y)   => NativePath.MoveTo(x, y);
    public void LineTo(float x, float y)   => NativePath.LineTo(x, y);
    public void Close()                    => NativePath.Close();
    public void Reset()                    => NativePath.Reset();

    public void CubicTo(float x1, float y1, float x2, float y2, float x3, float y3)
        => NativePath.CubicTo(x1, y1, x2, y2, x3, y3);

    public void QuadTo(float x1, float y1, float x2, float y2)
        => NativePath.QuadTo(x1, y1, x2, y2);

    public void ArcTo(Rect oval, float startAngle, float sweepAngle, bool forceMoveTo)
        => NativePath.ArcTo(SkiaCanvas.ToSk(oval), startAngle, sweepAngle, forceMoveTo);

    public void AddRect(Rect rect)
        => NativePath.AddRect(SkiaCanvas.ToSk(rect));

    public void AddRoundRect(RoundRect rrect)
        => NativePath.AddRoundRect(SkiaCanvas.ToSk(rrect));

    public void AddOval(Rect oval)
        => NativePath.AddOval(SkiaCanvas.ToSk(oval));

    public void Dispose() => NativePath.Dispose();
}
