using System.Runtime.InteropServices;

namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Matrix4x4"/>: identity, factories, right-to-left
/// composition, point vs direction transforms, Vulkan orthographic projection,
/// and the column-major memory layout contract.
/// </summary>
public sealed class Matrix4x4Tests
{
    [Test]
    public void Identity_LeavesVectorsUnchanged()
    {
        var v = new Vector4(1f, 2f, 3f, 4f);
        Assert.That(Matrix4x4.Identity * v, Is.EqualTo(v));
    }

    [Test]
    public void Identity_IsMultiplicationNeutral()
    {
        var m = Matrix4x4.CreateTranslation(new Vector3(1f, 2f, 3f));
        Assert.That(Matrix4x4.Identity * m, Is.EqualTo(m));
        Assert.That(m * Matrix4x4.Identity, Is.EqualTo(m));
    }

    [Test]
    public void Translation_MovesPoints_NotDirections()
    {
        var m = Matrix4x4.CreateTranslation(new Vector3(10f, 20f, 30f));
        Assert.That(m.TransformPoint(new Vector3(1f, 2f, 3f)), Is.EqualTo(new Vector3(11f, 22f, 33f)));
        Assert.That(m.TransformDirection(Vector3.Up), Is.EqualTo(Vector3.Up));
    }

    [Test]
    public void Scale_MultipliesComponents()
    {
        var m = Matrix4x4.CreateScale(new Vector3(2f, 3f, 4f));
        Assert.That(m.TransformPoint(new Vector3(1f, 2f, 3f)), Is.EqualTo(new Vector3(2f, 6f, 12f)));
        Assert.That(Matrix4x4.CreateScale(2f).TransformPoint(Vector3.One), Is.EqualTo(new Vector3(2f, 2f, 2f)));
    }

    [Test]
    public void RotationZ_QuarterTurn_MapsXToY()
    {
        var rotated = Matrix4x4.CreateRotationZ(MathF.PI / 2f).TransformPoint(Vector3.UnitX);
        Assert.That(rotated.X, Is.EqualTo(0f).Within(1e-5f));
        Assert.That(rotated.Y, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(rotated.Z, Is.EqualTo(0f).Within(1e-5f));
    }

    [Test]
    public void Composition_ReadsRightToLeft()
    {
        var translate = Matrix4x4.CreateTranslation(new Vector3(10f, 0f, 0f));
        var scale = Matrix4x4.CreateScale(2f);
        var point = Vector3.One;

        // T·S: scale first, then translate.
        Assert.That((translate * scale).TransformPoint(point), Is.EqualTo(new Vector3(12f, 2f, 2f)));
        // S·T: translate first, then scale — a different result.
        Assert.That((scale * translate).TransformPoint(point), Is.EqualTo(new Vector3(22f, 2f, 2f)));
    }

    [Test]
    public void Composition_MatchesSequentialTransforms()
    {
        var a = Matrix4x4.CreateRotationZ(0.7f);
        var b = Matrix4x4.CreateTranslation(new Vector3(3f, -1f, 2f));
        var v = new Vector4(1f, 2f, 3f, 1f);

        var composed = (a * b) * v;
        var sequential = a * (b * v);
        Assert.That(sequential.X, Is.EqualTo(composed.X).Within(1e-5f));
        Assert.That(sequential.Y, Is.EqualTo(composed.Y).Within(1e-5f));
        Assert.That(sequential.Z, Is.EqualTo(composed.Z).Within(1e-5f));
        Assert.That(sequential.W, Is.EqualTo(composed.W).Within(1e-5f));
    }

    [Test]
    public void Orthographic_MapsUiRectToVulkanClipSpace()
    {
        // UI space: origin top-left, Y down, 800×600, depth [0, 1].
        var ortho = Matrix4x4.CreateOrthographic(0f, 800f, 600f, 0f, 0f, 1f);

        // Top-left corner → (−1, −1): the top of Vulkan NDC (Y points down).
        Assert.That(ortho.TransformPoint(Vector3.Zero), Is.EqualTo(new Vector3(-1f, -1f, 0f)));
        // Bottom-right corner, far plane → (1, 1, 1).
        Assert.That(ortho.TransformPoint(new Vector3(800f, 600f, 1f)), Is.EqualTo(new Vector3(1f, 1f, 1f)));
        // Center, mid-depth → the middle of the clip volume.
        Assert.That(ortho.TransformPoint(new Vector3(400f, 300f, 0.5f)), Is.EqualTo(new Vector3(0f, 0f, 0.5f)));
    }

    [Test]
    public void MemoryLayout_IsColumnMajor_ReadyForGpuUpload()
    {
        var m = Matrix4x4.CreateTranslation(new Vector3(10f, 20f, 30f));
        Span<Matrix4x4> one = stackalloc Matrix4x4[] { m };
        var floats = MemoryMarshal.Cast<Matrix4x4, float>(one);

        Assert.That(floats.Length, Is.EqualTo(16));
        // Column 0 starts at float 0: M11 = 1 (identity diagonal).
        Assert.That(floats[0], Is.EqualTo(1f));
        // Translation lives in the fourth column: floats 12–14, as GLSL expects.
        Assert.That(floats[12], Is.EqualTo(10f));
        Assert.That(floats[13], Is.EqualTo(20f));
        Assert.That(floats[14], Is.EqualTo(30f));
        Assert.That(floats[15], Is.EqualTo(1f));
    }

    [Test]
    public void Equality_ComparesAllComponents()
    {
        var a = Matrix4x4.CreateTranslation(new Vector3(1f, 2f, 3f));
        var b = Matrix4x4.CreateTranslation(new Vector3(1f, 2f, 3f));
        Assert.That(a == b, Is.True);
        Assert.That(a != Matrix4x4.Identity, Is.True);
        Assert.That(b.GetHashCode(), Is.EqualTo(a.GetHashCode()));
    }
}
