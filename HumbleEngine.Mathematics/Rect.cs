namespace HumbleEngine;

/// <summary>
/// Immutable axis-aligned rectangle defined by a position (minimum corner on both
/// axes) and a size. Used for UI layout, clipping and hit-testing. Stays neutral
/// about the Y direction — interpreting the position as a top-left or bottom-left
/// corner is up to the consumer (the UI layer uses Y-down, origin at top-left).
/// Operations assume non-negative sizes.
/// </summary>
public readonly struct Rect : IEquatable<Rect>
{
    /// <summary>Minimum X of the rectangle.</summary>
    public float X { get; }

    /// <summary>Minimum Y of the rectangle.</summary>
    public float Y { get; }

    /// <summary>Extent along the X axis.</summary>
    public float Width { get; }

    /// <summary>Extent along the Y axis.</summary>
    public float Height { get; }

    /// <summary>Creates a rectangle from its minimum corner and dimensions.</summary>
    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>Creates a rectangle from its minimum corner and size as vectors.</summary>
    public Rect(Vector2 position, Vector2 size) : this(position.X, position.Y, size.X, size.Y)
    {
    }

    /// <summary>Rectangle at the origin with no area.</summary>
    public static Rect Zero => new(0f, 0f, 0f, 0f);

    /// <summary>Minimum corner of the rectangle.</summary>
    public Vector2 Position => new(X, Y);

    /// <summary>Dimensions of the rectangle.</summary>
    public Vector2 Size => new(Width, Height);

    /// <summary>Maximum corner of the rectangle: <see cref="Position"/> + <see cref="Size"/>.</summary>
    public Vector2 End => new(X + Width, Y + Height);

    /// <summary>Point at the middle of the rectangle.</summary>
    public Vector2 Center => new(X + Width / 2f, Y + Height / 2f);

    /// <summary>True when the rectangle covers no area (zero or negative extent on either axis).</summary>
    public bool IsEmpty => Width <= 0f || Height <= 0f;

    /// <summary>
    /// Whether the point lies inside, using the half-open convention: minimum edges
    /// are inside, maximum edges are outside. Two adjacent rectangles thus never
    /// both contain a point on their shared edge — no double hit in hit-testing.
    /// </summary>
    public bool Contains(Vector2 point) =>
        point.X >= X && point.X < X + Width &&
        point.Y >= Y && point.Y < Y + Height;

    /// <summary>Whether the two rectangles overlap on a non-zero area.</summary>
    public bool Intersects(Rect other) =>
        X < other.X + other.Width && other.X < X + Width &&
        Y < other.Y + other.Height && other.Y < Y + Height;

    /// <summary>
    /// Overlapping area of the two rectangles, or <see cref="Zero"/> when they
    /// do not overlap. Building block of UI clipping.
    /// </summary>
    public Rect Intersection(Rect other)
    {
        var minX = MathF.Max(X, other.X);
        var minY = MathF.Max(Y, other.Y);
        var maxX = MathF.Min(X + Width, other.X + other.Width);
        var maxY = MathF.Min(Y + Height, other.Y + other.Height);
        return maxX <= minX || maxY <= minY
            ? Zero
            : new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// Smallest rectangle containing both rectangles. Both are taken as-is:
    /// the union with an empty rectangle still extends to its position.
    /// </summary>
    public Rect Union(Rect other)
    {
        var minX = MathF.Min(X, other.X);
        var minY = MathF.Min(Y, other.Y);
        var maxX = MathF.Max(X + Width, other.X + other.Width);
        var maxY = MathF.Max(Y + Height, other.Y + other.Height);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>Copy of this rectangle moved by the given offset.</summary>
    public Rect Translated(Vector2 offset) => new(X + offset.X, Y + offset.Y, Width, Height);

    /// <summary>
    /// Copy of this rectangle grown by the given amount on all four sides
    /// (negative values shrink it). Building block of padding and margins.
    /// </summary>
    public Rect Grown(float amount) =>
        new(X - amount, Y - amount, Width + amount * 2f, Height + amount * 2f);

    /// <summary>Exact component equality. Beware float rounding on computed results.</summary>
    public static bool operator ==(Rect a, Rect b) => a.Equals(b);

    /// <summary>Exact component inequality.</summary>
    public static bool operator !=(Rect a, Rect b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(Rect other) =>
        X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Rect other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y}, {Width}×{Height})";
}