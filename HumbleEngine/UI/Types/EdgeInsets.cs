namespace HumbleEngine;

public readonly struct EdgeInsets : IEquatable<EdgeInsets>
{
    public float Top    { get; init; }
    public float Right  { get; init; }
    public float Bottom { get; init; }
    public float Left   { get; init; }

    public EdgeInsets(float top = 0, float right = 0, float bottom = 0, float left = 0)
    {
        Top    = top;
        Right  = right;
        Bottom = bottom;
        Left   = left;
    }

    public static EdgeInsets All(float value)                                     => new(value, value, value, value);
    public static EdgeInsets Symmetric(float horizontal = 0, float vertical = 0)  => new(vertical, horizontal, vertical, horizontal);

    public static readonly EdgeInsets Zero = new();

    public static implicit operator EdgeInsets(float value) => All(value);

    public bool Equals(EdgeInsets other) =>
        Top == other.Top && Right == other.Right && Bottom == other.Bottom && Left == other.Left;
    public override bool Equals(object? obj) => obj is EdgeInsets e && Equals(e);
    public override int GetHashCode() => HashCode.Combine(Top, Right, Bottom, Left);
    public static bool operator ==(EdgeInsets a, EdgeInsets b) => a.Equals(b);
    public static bool operator !=(EdgeInsets a, EdgeInsets b) => !a.Equals(b);
}
