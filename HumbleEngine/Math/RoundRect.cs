namespace HumbleEngine;

public readonly struct RoundRect
{
    public Rect  Rect    { get; }
    public float RadiusX { get; }
    public float RadiusY { get; }

    public RoundRect(Rect rect, float radiusX, float radiusY)
    {
        Rect    = rect;
        RadiusX = radiusX;
        RadiusY = radiusY;
    }

    public RoundRect(Rect rect, float radius) : this(rect, radius, radius) { }

    public static readonly RoundRect Empty = new(Rect.Empty, 0);

    public bool Equals(RoundRect other) => Rect == other.Rect && RadiusX == other.RadiusX && RadiusY == other.RadiusY;
    public override bool Equals(object? obj) => obj is RoundRect r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(Rect, RadiusX, RadiusY);
    public static bool operator ==(RoundRect a, RoundRect b) => a.Equals(b);
    public static bool operator !=(RoundRect a, RoundRect b) => !a.Equals(b);

    public override string ToString() => $"RoundRect({Rect}, rx={RadiusX}, ry={RadiusY})";
}
