namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Vector4"/>: homogeneous promotion, metrics,
/// operators and equality.
/// </summary>
public sealed class Vector4Tests
{
    [Fact]
    public void Constructor_StoresComponents()
    {
        var v = new Vector4(1f, -2f, 3f, 0.5f);
        Assert.Equal(1f, v.X);
        Assert.Equal(-2f, v.Y);
        Assert.Equal(3f, v.Z);
        Assert.Equal(0.5f, v.W);
    }

    [Fact]
    public void Vector3Constructor_PromotesPointOrDirection()
    {
        var p = new Vector3(1f, 2f, 3f);
        Assert.Equal(new Vector4(1f, 2f, 3f, 1f), new Vector4(p, 1f));
        Assert.Equal(new Vector4(1f, 2f, 3f, 0f), new Vector4(p, 0f));
    }

    [Fact]
    public void XYZ_DropsW()
    {
        Assert.Equal(new Vector3(1f, 2f, 3f), new Vector4(1f, 2f, 3f, 4f).XYZ);
    }

    [Fact]
    public void Length_OfTwoTwoTwoTwo_IsFour()
    {
        Assert.Equal(4f, new Vector4(2f, 2f, 2f, 2f).Length);
        Assert.Equal(16f, new Vector4(2f, 2f, 2f, 2f).LengthSquared);
    }

    [Fact]
    public void Dot_SumsComponentProducts()
    {
        var a = new Vector4(1f, 2f, 3f, 4f);
        var b = new Vector4(5f, 6f, 7f, 8f);
        Assert.Equal(70f, Vector4.Dot(a, b));
    }

    [Fact]
    public void Lerp_AtBoundsAndMidpoint()
    {
        var a = new Vector4(0f, 0f, 0f, 0f);
        var b = new Vector4(10f, -2f, 4f, 1f);
        Assert.Equal(a, Vector4.Lerp(a, b, 0f));
        Assert.Equal(b, Vector4.Lerp(a, b, 1f));
        Assert.Equal(new Vector4(5f, -1f, 2f, 0.5f), Vector4.Lerp(a, b, 0.5f));
    }

    [Fact]
    public void Operators_ComputeComponentWise()
    {
        var a = new Vector4(1f, 2f, 3f, 4f);
        var b = new Vector4(5f, 6f, 7f, 8f);
        Assert.Equal(new Vector4(6f, 8f, 10f, 12f), a + b);
        Assert.Equal(new Vector4(-4f, -4f, -4f, -4f), a - b);
        Assert.Equal(new Vector4(-1f, -2f, -3f, -4f), -a);
        Assert.Equal(new Vector4(5f, 12f, 21f, 32f), a * b);
        Assert.Equal(new Vector4(2f, 4f, 6f, 8f), a * 2f);
        Assert.Equal(new Vector4(2f, 4f, 6f, 8f), 2f * a);
        Assert.Equal(new Vector4(0.5f, 1f, 1.5f, 2f), a / 2f);
    }

    [Fact]
    public void Equality_ComparesComponents()
    {
        var a = new Vector4(1f, 2f, 3f, 4f);
        var b = new Vector4(1f, 2f, 3f, 4f);
        var c = new Vector4(1f, 2f, 3f, 5f);
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}