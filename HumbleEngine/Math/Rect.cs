namespace HumbleEngine;

public readonly struct Rect
{
    public float Left   { get; }
    public float Top    { get; }
    public float Right  { get; }
    public float Bottom { get; }

    public float Width  => Right  - Left;
    public float Height => Bottom - Top;

    public Vector2<float> Location => new(Left, Top);
    public Vector2<float> Size     => new(Width, Height);

    public Rect(float left, float top, float right, float bottom)
    {
        Left = left; Top = top; Right = right; Bottom = bottom;
    }

    public static Rect FromLTRB(float left, float top, float right, float bottom)
        => new(left, top, right, bottom);

    public static Rect FromXYWH(float x, float y, float width, float height)
        => new(x, y, x + width, y + height);

    public static readonly Rect Empty = new(0, 0, 0, 0);

    public bool Contains(float x, float y) => x >= Left && x <= Right && y >= Top && y <= Bottom;
    public bool Contains(Vector2<float> p)  => Contains(p.X, p.Y);

    public Rect Inflate(float dx, float dy) => new(Left - dx, Top - dy, Right + dx, Bottom + dy);
    public Rect Offset(float dx, float dy)  => new(Left + dx, Top + dy, Right + dx, Bottom + dy);

    public bool Equals(Rect other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
    public override bool Equals(object? obj) => obj is Rect r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);
    public static bool operator ==(Rect a, Rect b) => a.Equals(b);
    public static bool operator !=(Rect a, Rect b) => !a.Equals(b);

    public override string ToString() => $"Rect({Left},{Top},{Right},{Bottom})";
}
