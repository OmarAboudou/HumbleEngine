namespace HumbleEngine;

/// <summary>
/// Immutable 2D vector with single-precision components, used for positions,
/// sizes and directions in screen and 2D space.
/// </summary>
public readonly struct Vector2 : IEquatable<Vector2>
{
    /// <summary>Horizontal component.</summary>
    public float X { get; }

    /// <summary>Vertical component.</summary>
    public float Y { get; }

    /// <summary>Creates a vector from its two components.</summary>
    public Vector2(float x, float y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Vector with both components at zero.</summary>
    public static Vector2 Zero => new(0f, 0f);

    /// <summary>Vector with both components at one.</summary>
    public static Vector2 One => new(1f, 1f);

    /// <summary>Unit vector along the X axis.</summary>
    public static Vector2 UnitX => new(1f, 0f);

    /// <summary>Unit vector along the Y axis.</summary>
    public static Vector2 UnitY => new(0f, 1f);

    /// <summary>
    /// Euclidean length (magnitude) of the vector. Costs a square root —
    /// prefer <see cref="LengthSquared"/> when only comparing lengths.
    /// </summary>
    public float Length => MathF.Sqrt(X * X + Y * Y);

    /// <summary>Squared length of the vector. Cheaper than <see cref="Length"/>.</summary>
    public float LengthSquared => X * X + Y * Y;

    /// <summary>
    /// Copy of this vector scaled to length 1. The zero vector has no direction:
    /// normalizing it yields NaN components — guard with <see cref="LengthSquared"/> when in doubt.
    /// </summary>
    public Vector2 Normalized
    {
        get
        {
            var length = Length;
            return new Vector2(X / length, Y / length);
        }
    }

    /// <summary>
    /// Dot product of two vectors: |a|·|b|·cos(angle). Zero when perpendicular,
    /// positive when both point the same way, negative when opposed.
    /// </summary>
    public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;

    /// <summary>Euclidean distance between two points.</summary>
    public static float Distance(Vector2 a, Vector2 b) => (b - a).Length;

    /// <summary>Squared distance between two points. Cheaper than <see cref="Distance"/>.</summary>
    public static float DistanceSquared(Vector2 a, Vector2 b) => (b - a).LengthSquared;

    /// <summary>
    /// Linear interpolation from <paramref name="a"/> to <paramref name="b"/>:
    /// t = 0 returns <paramref name="a"/>, t = 1 returns <paramref name="b"/>.
    /// Not clamped outside [0, 1].
    /// </summary>
    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * t;

    /// <summary>Component-wise addition.</summary>
    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);

    /// <summary>Component-wise subtraction.</summary>
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>Negation: same length, opposite direction.</summary>
    public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);

    /// <summary>Uniform scaling.</summary>
    public static Vector2 operator *(Vector2 v, float scalar) => new(v.X * scalar, v.Y * scalar);

    /// <summary>Uniform scaling.</summary>
    public static Vector2 operator *(float scalar, Vector2 v) => v * scalar;

    /// <summary>Component-wise multiplication (Hadamard product).</summary>
    public static Vector2 operator *(Vector2 a, Vector2 b) => new(a.X * b.X, a.Y * b.Y);

    /// <summary>Uniform inverse scaling.</summary>
    public static Vector2 operator /(Vector2 v, float scalar) => new(v.X / scalar, v.Y / scalar);

    /// <summary>Component-wise division.</summary>
    public static Vector2 operator /(Vector2 a, Vector2 b) => new(a.X / b.X, a.Y / b.Y);

    /// <summary>Exact component equality. Beware float rounding on computed results.</summary>
    public static bool operator ==(Vector2 a, Vector2 b) => a.Equals(b);

    /// <summary>Exact component inequality.</summary>
    public static bool operator !=(Vector2 a, Vector2 b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(Vector2 other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vector2 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y})";
}
