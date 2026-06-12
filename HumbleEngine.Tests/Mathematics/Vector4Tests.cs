namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Vector4"/>: homogeneous promotion, metrics,
/// operators and equality.
/// </summary>
public sealed class Vector4Tests
{
    [Test]
    public void Constructor_StoresComponents()
    {
        var v = new Vector4(1f, -2f, 3f, 0.5f);
        Assert.That(v.X, Is.EqualTo(1f));
        Assert.That(v.Y, Is.EqualTo(-2f));
        Assert.That(v.Z, Is.EqualTo(3f));
        Assert.That(v.W, Is.EqualTo(0.5f));
    }

    [Test]
    public void Vector3Constructor_PromotesPointOrDirection()
    {
        var p = new Vector3(1f, 2f, 3f);
        Assert.That(new Vector4(p, 1f), Is.EqualTo(new Vector4(1f, 2f, 3f, 1f)));
        Assert.That(new Vector4(p, 0f), Is.EqualTo(new Vector4(1f, 2f, 3f, 0f)));
    }

    [Test]
    public void XYZ_DropsW()
    {
        Assert.That(new Vector4(1f, 2f, 3f, 4f).XYZ, Is.EqualTo(new Vector3(1f, 2f, 3f)));
    }

    [Test]
    public void Length_OfTwoTwoTwoTwo_IsFour()
    {
        Assert.That(new Vector4(2f, 2f, 2f, 2f).Length, Is.EqualTo(4f));
        Assert.That(new Vector4(2f, 2f, 2f, 2f).LengthSquared, Is.EqualTo(16f));
    }

    [Test]
    public void Dot_SumsComponentProducts()
    {
        var a = new Vector4(1f, 2f, 3f, 4f);
        var b = new Vector4(5f, 6f, 7f, 8f);
        Assert.That(Vector4.Dot(a, b), Is.EqualTo(70f));
    }

    [Test]
    public void Lerp_AtBoundsAndMidpoint()
    {
        var a = new Vector4(0f, 0f, 0f, 0f);
        var b = new Vector4(10f, -2f, 4f, 1f);
        Assert.That(Vector4.Lerp(a, b, 0f), Is.EqualTo(a));
        Assert.That(Vector4.Lerp(a, b, 1f), Is.EqualTo(b));
        Assert.That(Vector4.Lerp(a, b, 0.5f), Is.EqualTo(new Vector4(5f, -1f, 2f, 0.5f)));
    }

    [Test]
    public void Operators_ComputeComponentWise()
    {
        var a = new Vector4(1f, 2f, 3f, 4f);
        var b = new Vector4(5f, 6f, 7f, 8f);
        Assert.That(a + b, Is.EqualTo(new Vector4(6f, 8f, 10f, 12f)));
        Assert.That(a - b, Is.EqualTo(new Vector4(-4f, -4f, -4f, -4f)));
        Assert.That(-a, Is.EqualTo(new Vector4(-1f, -2f, -3f, -4f)));
        Assert.That(a * b, Is.EqualTo(new Vector4(5f, 12f, 21f, 32f)));
        Assert.That(a * 2f, Is.EqualTo(new Vector4(2f, 4f, 6f, 8f)));
        Assert.That(2f * a, Is.EqualTo(new Vector4(2f, 4f, 6f, 8f)));
        Assert.That(a / 2f, Is.EqualTo(new Vector4(0.5f, 1f, 1.5f, 2f)));
    }

    [Test]
    public void Equality_ComparesComponents()
    {
        var a = new Vector4(1f, 2f, 3f, 4f);
        var b = new Vector4(1f, 2f, 3f, 4f);
        var c = new Vector4(1f, 2f, 3f, 5f);
        Assert.That(a == b, Is.True);
        Assert.That(a != c, Is.True);
        Assert.That(b.GetHashCode(), Is.EqualTo(a.GetHashCode()));
    }
}
