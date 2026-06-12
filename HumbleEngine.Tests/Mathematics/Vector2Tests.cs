namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Vector2"/>: constants, metrics, operators and equality.
/// </summary>
public sealed class Vector2Tests
{
    [Fact]
    public void Constructor_StoresComponents()
    {
        var v = new Vector2(3f, -4f);
        Assert.Equal(3f, v.X);
        Assert.Equal(-4f, v.Y);
    }

    [Fact]
    public void Constants_HaveExpectedComponents()
    {
        Assert.Equal(new Vector2(0f, 0f), Vector2.Zero);
        Assert.Equal(new Vector2(1f, 1f), Vector2.One);
        Assert.Equal(new Vector2(1f, 0f), Vector2.UnitX);
        Assert.Equal(new Vector2(0f, 1f), Vector2.UnitY);
    }

    [Fact]
    public void Length_OfThreeFour_IsFive()
    {
        Assert.Equal(5f, new Vector2(3f, 4f).Length);
    }

    [Fact]
    public void LengthSquared_AvoidsSquareRoot()
    {
        Assert.Equal(25f, new Vector2(3f, 4f).LengthSquared);
    }

    [Fact]
    public void Normalized_HasLengthOne_AndKeepsDirection()
    {
        var n = new Vector2(3f, 4f).Normalized;
        Assert.Equal(1f, n.Length, 5);
        Assert.Equal(0.6f, n.X, 5);
        Assert.Equal(0.8f, n.Y, 5);
    }

    [Fact]
    public void Normalized_ZeroVector_YieldsNaN()
    {
        var n = Vector2.Zero.Normalized;
        Assert.True(float.IsNaN(n.X));
        Assert.True(float.IsNaN(n.Y));
    }

    [Fact]
    public void Dot_PerpendicularVectors_IsZero()
    {
        Assert.Equal(0f, Vector2.Dot(Vector2.UnitX, Vector2.UnitY));
    }

    [Fact]
    public void Dot_OpposedVectors_IsNegative()
    {
        Assert.True(Vector2.Dot(Vector2.UnitX, -Vector2.UnitX) < 0f);
    }

    [Fact]
    public void Distance_BetweenPoints_IsEuclidean()
    {
        Assert.Equal(5f, Vector2.Distance(new Vector2(1f, 1f), new Vector2(4f, 5f)));
    }

    [Fact]
    public void Lerp_AtBoundsAndMidpoint()
    {
        var a = new Vector2(0f, 0f);
        var b = new Vector2(10f, -2f);
        Assert.Equal(a, Vector2.Lerp(a, b, 0f));
        Assert.Equal(b, Vector2.Lerp(a, b, 1f));
        Assert.Equal(new Vector2(5f, -1f), Vector2.Lerp(a, b, 0.5f));
    }

    [Fact]
    public void Operators_ComputeComponentWise()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(3f, 4f);
        Assert.Equal(new Vector2(4f, 6f), a + b);
        Assert.Equal(new Vector2(-2f, -2f), a - b);
        Assert.Equal(new Vector2(-1f, -2f), -a);
        Assert.Equal(new Vector2(3f, 8f), a * b);
        Assert.Equal(new Vector2(2f, 4f), a * 2f);
        Assert.Equal(new Vector2(2f, 4f), 2f * a);
        Assert.Equal(new Vector2(0.5f, 1f), a / 2f);
        Assert.Equal(new Vector2(3f, 2f), b / a);
    }

    [Fact]
    public void Equality_ComparesComponents()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(1f, 2f);
        var c = new Vector2(1f, 3f);
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
