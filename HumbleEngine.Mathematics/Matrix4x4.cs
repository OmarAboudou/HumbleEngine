using System.Runtime.InteropServices;

namespace HumbleEngine;

/// <summary>
/// Immutable 4×4 matrix with single-precision components, using the column-vector
/// convention: a vector transforms as v' = M·v, and compositions read right to
/// left (T·R·S scales first, translates last).
/// <para>
/// Field names follow math notation (M&lt;row&gt;&lt;column&gt;), but the fields are
/// <b>declared column by column</b> so the memory layout is column-major — exactly
/// what GLSL/Vulkan (std140) expects: a matrix uploads to the GPU as a raw copy,
/// translation in floats 12–14.
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Matrix4x4 : IEquatable<Matrix4x4>
{
    // Column 0
    /// <summary>Row 1, column 1.</summary>
    public readonly float M11;
    /// <summary>Row 2, column 1.</summary>
    public readonly float M21;
    /// <summary>Row 3, column 1.</summary>
    public readonly float M31;
    /// <summary>Row 4, column 1.</summary>
    public readonly float M41;

    // Column 1
    /// <summary>Row 1, column 2.</summary>
    public readonly float M12;
    /// <summary>Row 2, column 2.</summary>
    public readonly float M22;
    /// <summary>Row 3, column 2.</summary>
    public readonly float M32;
    /// <summary>Row 4, column 2.</summary>
    public readonly float M42;

    // Column 2
    /// <summary>Row 1, column 3.</summary>
    public readonly float M13;
    /// <summary>Row 2, column 3.</summary>
    public readonly float M23;
    /// <summary>Row 3, column 3.</summary>
    public readonly float M33;
    /// <summary>Row 4, column 3.</summary>
    public readonly float M43;

    // Column 3
    /// <summary>Row 1, column 4 — X translation in an affine transform.</summary>
    public readonly float M14;
    /// <summary>Row 2, column 4 — Y translation in an affine transform.</summary>
    public readonly float M24;
    /// <summary>Row 3, column 4 — Z translation in an affine transform.</summary>
    public readonly float M34;
    /// <summary>Row 4, column 4.</summary>
    public readonly float M44;

    /// <summary>
    /// Creates a matrix from its sixteen components, given <b>row by row</b> —
    /// the arguments read like the matrix written on paper, regardless of the
    /// column-major storage.
    /// </summary>
    public Matrix4x4(
        float m11, float m12, float m13, float m14,
        float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34,
        float m41, float m42, float m43, float m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    /// <summary>Matrix that leaves vectors unchanged.</summary>
    public static Matrix4x4 Identity => new(
        1f, 0f, 0f, 0f,
        0f, 1f, 0f, 0f,
        0f, 0f, 1f, 0f,
        0f, 0f, 0f, 1f);

    /// <summary>Matrix that moves points by the given offset (directions are unaffected).</summary>
    public static Matrix4x4 CreateTranslation(Vector3 offset) => new(
        1f, 0f, 0f, offset.X,
        0f, 1f, 0f, offset.Y,
        0f, 0f, 1f, offset.Z,
        0f, 0f, 0f, 1f);

    /// <summary>Matrix that scales each axis by the given factors.</summary>
    public static Matrix4x4 CreateScale(Vector3 scale) => new(
        scale.X, 0f, 0f, 0f,
        0f, scale.Y, 0f, 0f,
        0f, 0f, scale.Z, 0f,
        0f, 0f, 0f, 1f);

    /// <summary>Matrix that scales all axes uniformly.</summary>
    public static Matrix4x4 CreateScale(float scale) => CreateScale(new Vector3(scale, scale, scale));

    /// <summary>
    /// Matrix that rotates around the Z axis by the given angle in radians.
    /// Positive angles rotate from +X toward +Y (counter-clockwise when looking
    /// from +Z, per the right-hand rule). This is the 2D/UI rotation.
    /// </summary>
    public static Matrix4x4 CreateRotationZ(float radians)
    {
        var c = MathF.Cos(radians);
        var s = MathF.Sin(radians);
        return new Matrix4x4(
            c, -s, 0f, 0f,
            s, c, 0f, 0f,
            0f, 0f, 1f, 0f,
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// Orthographic projection mapping the box [left, right] × [top, bottom] ×
    /// [near, far] to the Vulkan clip space: X to [−1, 1], Y to [−1, 1] with
    /// <paramref name="top"/> at −1 (Vulkan NDC Y points down), Z to [0, 1].
    /// This matrix is the single place absorbing the Vulkan clip conventions.
    /// For UI rendering: <c>CreateOrthographic(0, width, height, 0, 0, 1)</c>
    /// puts the origin at the top-left of the framebuffer.
    /// </summary>
    public static Matrix4x4 CreateOrthographic(
        float left, float right, float bottom, float top, float near, float far) => new(
        2f / (right - left), 0f, 0f, -(right + left) / (right - left),
        0f, 2f / (bottom - top), 0f, -(bottom + top) / (bottom - top),
        0f, 0f, 1f / (far - near), -near / (far - near),
        0f, 0f, 0f, 1f);

    /// <summary>
    /// Matrix composition. With column vectors, (a · b) · v applies
    /// <paramref name="b"/> first, then <paramref name="a"/> — read right to left.
    /// </summary>
    public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b) => new(
        a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
        a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
        a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
        a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,
        a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
        a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
        a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
        a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,
        a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
        a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
        a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
        a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,
        a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
        a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
        a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
        a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44);

    /// <summary>Transforms a vector: v' = M·v (column-vector convention).</summary>
    public static Vector4 operator *(Matrix4x4 m, Vector4 v) => new(
        m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z + m.M14 * v.W,
        m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z + m.M24 * v.W,
        m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z + m.M34 * v.W,
        m.M41 * v.X + m.M42 * v.Y + m.M43 * v.Z + m.M44 * v.W);

    /// <summary>
    /// Transforms a point (homogeneous W = 1): affected by translation.
    /// Assumes an affine matrix — no perspective divide is performed.
    /// </summary>
    public Vector3 TransformPoint(Vector3 point) => (this * new Vector4(point, 1f)).XYZ;

    /// <summary>
    /// Transforms a direction (homogeneous W = 0): rotated and scaled,
    /// never translated — "up" stays "up" wherever the object sits.
    /// </summary>
    public Vector3 TransformDirection(Vector3 direction) => (this * new Vector4(direction, 0f)).XYZ;

    /// <summary>Exact component equality. Beware float rounding on computed results.</summary>
    public static bool operator ==(Matrix4x4 a, Matrix4x4 b) => a.Equals(b);

    /// <summary>Exact component inequality.</summary>
    public static bool operator !=(Matrix4x4 a, Matrix4x4 b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(Matrix4x4 other) =>
        M11 == other.M11 && M12 == other.M12 && M13 == other.M13 && M14 == other.M14 &&
        M21 == other.M21 && M22 == other.M22 && M23 == other.M23 && M24 == other.M24 &&
        M31 == other.M31 && M32 == other.M32 && M33 == other.M33 && M34 == other.M34 &&
        M41 == other.M41 && M42 == other.M42 && M43 == other.M43 && M44 == other.M44;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Matrix4x4 other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(M11); hash.Add(M12); hash.Add(M13); hash.Add(M14);
        hash.Add(M21); hash.Add(M22); hash.Add(M23); hash.Add(M24);
        hash.Add(M31); hash.Add(M32); hash.Add(M33); hash.Add(M34);
        hash.Add(M41); hash.Add(M42); hash.Add(M43); hash.Add(M44);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() =>
        $"[{M11} {M12} {M13} {M14} | {M21} {M22} {M23} {M24} | {M31} {M32} {M33} {M34} | {M41} {M42} {M43} {M44}]";
}