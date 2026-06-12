namespace HumbleEngine;

/// <summary>
/// Immutable 4D vector with single-precision components. Two main uses:
/// homogeneous coordinates for matrix transforms (W = 1 for points, W = 0 for
/// directions) and RGBA colors (W as alpha).
/// </summary>
public readonly struct Vector4 : IEquatable<Vector4>
{
    /// <summary>First component (X, or red).</summary>
    public float X { get; }

    /// <summary>Second component (Y, or green).</summary>
    public float Y { get; }

    /// <summary>Third component (Z, or blue).</summary>
    public float Z { get; }

    /// <summary>Fourth component (homogeneous W, or alpha).</summary>
    public float W { get; }

    /// <summary>Creates a vector from its four components.</summary>
    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    /// <summary>
    /// Creates a vector from a 3D vector and a W component — the usual way to
    /// promote a point (w = 1) or a direction (w = 0) before a matrix transform.
    /// </summary>
    public Vector4(Vector3 xyz, float w) : this(xyz.X, xyz.Y, xyz.Z, w)
    {
    }

    /// <summary>Vector with all components at zero.</summary>
    public static Vector4 Zero => new(0f, 0f, 0f, 0f);

    /// <summary>Vector with all components at one.</summary>
    public static Vector4 One => new(1f, 1f, 1f, 1f);

    /// <summary>The X, Y and Z components as a 3D vector, dropping W.</summary>
    public Vector3 XYZ => new(X, Y, Z);

    /// <summary>
    /// Euclidean length (magnitude) of the vector. Costs a square root —
    /// prefer <see cref="LengthSquared"/> when only comparing lengths.
    /// </summary>
    public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z + W * W);

    /// <summary>Squared length of the vector. Cheaper than <see cref="Length"/>.</summary>
    public float LengthSquared => X * X + Y * Y + Z * Z + W * W;

    /// <summary>Dot product of two vectors, component-wise products summed.</summary>
    public static float Dot(Vector4 a, Vector4 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;

    /// <summary>
    /// Linear interpolation from <paramref name="a"/> to <paramref name="b"/>:
    /// t = 0 returns <paramref name="a"/>, t = 1 returns <paramref name="b"/>.
    /// Not clamped outside [0, 1]. Also blends colors.
    /// </summary>
    public static Vector4 Lerp(Vector4 a, Vector4 b, float t) => a + (b - a) * t;

    /// <summary>Component-wise addition.</summary>
    public static Vector4 operator +(Vector4 a, Vector4 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

    /// <summary>Component-wise subtraction.</summary>
    public static Vector4 operator -(Vector4 a, Vector4 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);

    /// <summary>Negation of every component.</summary>
    public static Vector4 operator -(Vector4 v) => new(-v.X, -v.Y, -v.Z, -v.W);

    /// <summary>Uniform scaling.</summary>
    public static Vector4 operator *(Vector4 v, float scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar, v.W * scalar);

    /// <summary>Uniform scaling.</summary>
    public static Vector4 operator *(float scalar, Vector4 v) => v * scalar;

    /// <summary>Component-wise multiplication (Hadamard product) — also color modulation.</summary>
    public static Vector4 operator *(Vector4 a, Vector4 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);

    /// <summary>Uniform inverse scaling.</summary>
    public static Vector4 operator /(Vector4 v, float scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar, v.W / scalar);

    /// <summary>Exact component equality. Beware float rounding on computed results.</summary>
    public static bool operator ==(Vector4 a, Vector4 b) => a.Equals(b);

    /// <summary>Exact component inequality.</summary>
    public static bool operator !=(Vector4 a, Vector4 b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(Vector4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vector4 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y}, {Z}, {W})";
}