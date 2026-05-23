namespace HumbleEngine;

public readonly struct CornerRadius : IEquatable<CornerRadius>
{
    public float TopLeft     { get; init; }
    public float TopRight    { get; init; }
    public float BottomRight { get; init; }
    public float BottomLeft  { get; init; }

    public CornerRadius(float topLeft = 0, float topRight = 0, float bottomRight = 0, float bottomLeft = 0)
    {
        TopLeft     = topLeft;
        TopRight    = topRight;
        BottomRight = bottomRight;
        BottomLeft  = bottomLeft;
    }

    public static CornerRadius All(float value) => new(value, value, value, value);

    public static readonly CornerRadius Zero = new();

    public static implicit operator CornerRadius(float value) => All(value);

    public bool Equals(CornerRadius other) =>
        TopLeft == other.TopLeft && TopRight == other.TopRight &&
        BottomRight == other.BottomRight && BottomLeft == other.BottomLeft;
    public override bool Equals(object? obj) => obj is CornerRadius c && Equals(c);
    public override int GetHashCode() => HashCode.Combine(TopLeft, TopRight, BottomRight, BottomLeft);
    public static bool operator ==(CornerRadius a, CornerRadius b) => a.Equals(b);
    public static bool operator !=(CornerRadius a, CornerRadius b) => !a.Equals(b);
}
