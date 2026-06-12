namespace HumbleEngine;

/// <summary>
/// Immutable 3D vector with single-precision components, used for positions,
/// directions and scales in 3D space.
/// </summary>
public readonly struct Vector3 : IEquatable<Vector3>
{
    /// <summary>Component along the X axis.</summary>
    public float X { get; }

    /// <summary>Component along the Y axis.</summary>
    public float Y { get; }

    /// <summary>Component along the Z axis.</summary>
    public float Z { get; }

    /// <summary>Creates a vector from its three components.</summary>
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Creates a vector from a 2D vector and a Z component.</summary>
    public Vector3(Vector2 xy, float z) : this(xy.X, xy.Y, z)
    {
    }

    /// <summary>Vector with all components at zero.</summary>
    public static Vector3 Zero => new(0f, 0f, 0f);

    /// <summary>Vector with all components at one.</summary>
    public static Vector3 One => new(1f, 1f, 1f);

    /// <summary>Unit vector along the X axis.</summary>
    public static Vector3 UnitX => new(1f, 0f, 0f);

    /// <summary>Unit vector along the Y axis.</summary>
    public static Vector3 UnitY => new(0f, 1f, 0f);

    /// <summary>Unit vector along the Z axis.</summary>
    public static Vector3 UnitZ => new(0f, 0f, 1f);

    /// <summary>Right direction in the engine convention (+X).</summary>
    public static Vector3 Right => new(1f, 0f, 0f);

    /// <summary>Up direction in the engine convention (+Y).</summary>
    public static Vector3 Up => new(0f, 1f, 0f);

    /// <summary>
    /// Forward direction in the engine convention (−Z): right-handed, Y up,
    /// the camera looks toward −Z (Godot/OpenGL/glTF convention).
    /// </summary>
    public static Vector3 Forward => new(0f, 0f, -1f);

    /// <summary>
    /// Euclidean length (magnitude) of the vector. Costs a square root —
    /// prefer <see cref="LengthSquared"/> when only comparing lengths.
    /// </summary>
    public float Length => MathF.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>Squared length of the vector. Cheaper than <see cref="Length"/>.</summary>
    public float LengthSquared => X * X + Y * Y + Z * Z;

    /// <summary>
    /// Copy of this vector scaled to length 1. The zero vector has no direction:
    /// normalizing it yields NaN components — guard with <see cref="LengthSquared"/> when in doubt.
    /// </summary>
    public Vector3 Normalized
    {
        get
        {
            var length = Length;
            return new Vector3(X / length, Y / length, Z / length);
        }
    }

    /// <summary>
    /// Dot product of two vectors: |a|·|b|·cos(angle). Zero when perpendicular,
    /// positive when both point the same way, negative when opposed.
    /// </summary>
    public static float Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>
    /// Cross product: vector perpendicular to both operands, of length
    /// |a|·|b|·sin(angle), oriented by the right-hand rule (X × Y = Z).
    /// Anticommutative: b × a = −(a × b).
    /// </summary>
    public static Vector3 Cross(Vector3 a, Vector3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    /// <summary>Euclidean distance between two points.</summary>
    public static float Distance(Vector3 a, Vector3 b) => (b - a).Length;

    /// <summary>Squared distance between two points. Cheaper than <see cref="Distance"/>.</summary>
    public static float DistanceSquared(Vector3 a, Vector3 b) => (b - a).LengthSquared;

    /// <summary>
    /// Linear interpolation from <paramref name="a"/> to <paramref name="b"/>:
    /// t = 0 returns <paramref name="a"/>, t = 1 returns <paramref name="b"/>.
    /// Not clamped outside [0, 1].
    /// </summary>
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a + (b - a) * t;

    /// <summary>Component-wise addition.</summary>
    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    /// <summary>Component-wise subtraction.</summary>
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    /// <summary>Negation: same length, opposite direction.</summary>
    public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);

    /// <summary>Uniform scaling.</summary>
    public static Vector3 operator *(Vector3 v, float scalar) => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    /// <summary>Uniform scaling.</summary>
    public static Vector3 operator *(float scalar, Vector3 v) => v * scalar;

    /// <summary>Component-wise multiplication (Hadamard product).</summary>
    public static Vector3 operator *(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    /// <summary>Uniform inverse scaling.</summary>
    public static Vector3 operator /(Vector3 v, float scalar) => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

    /// <summary>Component-wise division.</summary>
    public static Vector3 operator /(Vector3 a, Vector3 b) => new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

    /// <summary>Exact component equality. Beware float rounding on computed results.</summary>
    public static bool operator ==(Vector3 a, Vector3 b) => a.Equals(b);

    /// <summary>Exact component inequality.</summary>
    public static bool operator !=(Vector3 a, Vector3 b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y}, {Z})";
}