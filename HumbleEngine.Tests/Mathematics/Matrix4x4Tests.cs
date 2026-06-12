using System.Runtime.InteropServices;

namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Matrix4x4"/>: identity, factories, right-to-left
/// composition, point vs direction transforms, Vulkan orthographic projection,
/// and the column-major memory layout contract.
/// </summary>
public sealed class Matrix4x4Tests
{
    [Fact]
    public void Identity_LeavesVectorsUnchanged()
    {
        var v = new Vector4(1f, 2f, 3f, 4f);
        Assert.Equal(v, Matrix4x4.Identity * v);
    }

    [Fact]
    public void Identity_IsMultiplicationNeutral()
    {
        var m = Matrix4x4.CreateTranslation(new Vector3(1f, 2f, 3f));
        Assert.Equal(m, Matrix4x4.Identity * m);
        Assert.Equal(m, m * Matrix4x4.Identity);
    }

    [Fact]
    public void Translation_MovesPoints_NotDirections()
    {
        var m = Matrix4x4.CreateTranslation(new Vector3(10f, 20f, 30f));
        Assert.Equal(new Vector3(11f, 22f, 33f), m.TransformPoint(new Vector3(1f, 2f, 3f)));
        Assert.Equal(Vector3.Up, m.TransformDirection(Vector3.Up));
    }

    [Fact]
    public void Scale_MultipliesComponents()
    {
        var m = Matrix4x4.CreateScale(new Vector3(2f, 3f, 4f));
        Assert.Equal(new Vector3(2f, 6f, 12f), m.TransformPoint(new Vector3(1f, 2f, 3f)));
        Assert.Equal(new Vector3(2f, 2f, 2f), Matrix4x4.CreateScale(2f).TransformPoint(Vector3.One));
    }

    [Fact]
    public void RotationZ_QuarterTurn_MapsXToY()
    {
        var rotated = Matrix4x4.CreateRotationZ(MathF.PI / 2f).TransformPoint(Vector3.UnitX);
        Assert.Equal(0f, rotated.X, 5);
        Assert.Equal(1f, rotated.Y, 5);
        Assert.Equal(0f, rotated.Z, 5);
    }

    [Fact]
    public void Composition_ReadsRightToLeft()
    {
        var translate = Matrix4x4.CreateTranslation(new Vector3(10f, 0f, 0f));
        var scale = Matrix4x4.CreateScale(2f);
        var point = Vector3.One;

        // T·S: scale first, then translate.
        Assert.Equal(new Vector3(12f, 2f, 2f), (translate * scale).TransformPoint(point));
        // S·T: translate first, then scale — a different result.
        Assert.Equal(new Vector3(22f, 2f, 2f), (scale * translate).TransformPoint(point));
    }

    [Fact]
    public void Composition_MatchesSequentialTransforms()
    {
        var a = Matrix4x4.CreateRotationZ(0.7f);
        var b = Matrix4x4.CreateTranslation(new Vector3(3f, -1f, 2f));
        var v = new Vector4(1f, 2f, 3f, 1f);

        var composed = (a * b) * v;
        var sequential = a * (b * v);
        Assert.Equal(composed.X, sequential.X, 5);
        Assert.Equal(composed.Y, sequential.Y, 5);
        Assert.Equal(composed.Z, sequential.Z, 5);
        Assert.Equal(composed.W, sequential.W, 5);
    }

    [Fact]
    public void Orthographic_MapsUiRectToVulkanClipSpace()
    {
        // UI space: origin top-left, Y down, 800×600, depth [0, 1].
        var ortho = Matrix4x4.CreateOrthographic(0f, 800f, 600f, 0f, 0f, 1f);

        // Top-left corner → (−1, −1): the top of Vulkan NDC (Y points down).
        Assert.Equal(new Vector3(-1f, -1f, 0f), ortho.TransformPoint(Vector3.Zero));
        // Bottom-right corner, far plane → (1, 1, 1).
        Assert.Equal(new Vector3(1f, 1f, 1f), ortho.TransformPoint(new Vector3(800f, 600f, 1f)));
        // Center, mid-depth → the middle of the clip volume.
        Assert.Equal(new Vector3(0f, 0f, 0.5f), ortho.TransformPoint(new Vector3(400f, 300f, 0.5f)));
    }

    [Fact]
    public void MemoryLayout_IsColumnMajor_ReadyForGpuUpload()
    {
        var m = Matrix4x4.CreateTranslation(new Vector3(10f, 20f, 30f));
        Span<Matrix4x4> one = stackalloc Matrix4x4[] { m };
        var floats = MemoryMarshal.Cast<Matrix4x4, float>(one);

        Assert.Equal(16, floats.Length);
        // Column 0 starts at float 0: M11 = 1 (identity diagonal).
        Assert.Equal(1f, floats[0]);
        // Translation lives in the fourth column: floats 12–14, as GLSL expects.
        Assert.Equal(10f, floats[12]);
        Assert.Equal(20f, floats[13]);
        Assert.Equal(30f, floats[14]);
        Assert.Equal(1f, floats[15]);
    }

    [Fact]
    public void Equality_ComparesAllComponents()
    {
        var a = Matrix4x4.CreateTranslation(new Vector3(1f, 2f, 3f));
        var b = Matrix4x4.CreateTranslation(new Vector3(1f, 2f, 3f));
        Assert.True(a == b);
        Assert.True(a != Matrix4x4.Identity);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}