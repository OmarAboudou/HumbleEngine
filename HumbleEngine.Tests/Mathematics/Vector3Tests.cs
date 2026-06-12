namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Vector3"/>: constants, engine directions,
/// metrics, cross product, operators and equality.
/// </summary>
public sealed class Vector3Tests
{
    [Fact]
    public void Constructor_StoresComponents()
    {
        var v = new Vector3(1f, -2f, 3f);
        Assert.Equal(1f, v.X);
        Assert.Equal(-2f, v.Y);
        Assert.Equal(3f, v.Z);
    }

    [Fact]
    public void Vector2Constructor_AppendsZ()
    {
        Assert.Equal(new Vector3(1f, 2f, 3f), new Vector3(new Vector2(1f, 2f), 3f));
    }

    [Fact]
    public void EngineDirections_FollowRightHandedYUpMinusZForward()
    {
        Assert.Equal(Vector3.UnitX, Vector3.Right);
        Assert.Equal(Vector3.UnitY, Vector3.Up);
        Assert.Equal(new Vector3(0f, 0f, -1f), Vector3.Forward);
    }

    [Fact]
    public void Length_OfOneTwoTwo_IsThree()
    {
        Assert.Equal(3f, new Vector3(1f, 2f, 2f).Length);
        Assert.Equal(9f, new Vector3(1f, 2f, 2f).LengthSquared);
    }

    [Fact]
    public void Normalized_HasLengthOne()
    {
        Assert.Equal(1f, new Vector3(1f, 2f, 2f).Normalized.Length, 5);
    }

    [Fact]
    public void Dot_PerpendicularVectors_IsZero()
    {
        Assert.Equal(0f, Vector3.Dot(Vector3.UnitX, Vector3.UnitY));
    }

    [Fact]
    public void Cross_FollowsRightHandRule()
    {
        Assert.Equal(Vector3.UnitZ, Vector3.Cross(Vector3.UnitX, Vector3.UnitY));
        Assert.Equal(Vector3.UnitX, Vector3.Cross(Vector3.UnitY, Vector3.UnitZ));
        Assert.Equal(Vector3.UnitY, Vector3.Cross(Vector3.UnitZ, Vector3.UnitX));
    }

    [Fact]
    public void Cross_IsAnticommutative()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(4f, 5f, 6f);
        Assert.Equal(Vector3.Cross(a, b), -Vector3.Cross(b, a));
    }

    [Fact]
    public void Cross_IsPerpendicularToBothOperands()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(4f, 5f, 6f);
        var c = Vector3.Cross(a, b);
        Assert.Equal(0f, Vector3.Dot(c, a), 5);
        Assert.Equal(0f, Vector3.Dot(c, b), 5);
    }

    [Fact]
    public void Lerp_AtBoundsAndMidpoint()
    {
        var a = new Vector3(0f, 0f, 0f);
        var b = new Vector3(10f, -2f, 4f);
        Assert.Equal(a, Vector3.Lerp(a, b, 0f));
        Assert.Equal(b, Vector3.Lerp(a, b, 1f));
        Assert.Equal(new Vector3(5f, -1f, 2f), Vector3.Lerp(a, b, 0.5f));
    }

    [Fact]
    public void Operators_ComputeComponentWise()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(4f, 6f, 8f);
        Assert.Equal(new Vector3(5f, 8f, 11f), a + b);
        Assert.Equal(new Vector3(-3f, -4f, -5f), a - b);
        Assert.Equal(new Vector3(-1f, -2f, -3f), -a);
        Assert.Equal(new Vector3(4f, 12f, 24f), a * b);
        Assert.Equal(new Vector3(2f, 4f, 6f), a * 2f);
        Assert.Equal(new Vector3(2f, 4f, 6f), 2f * a);
        Assert.Equal(new Vector3(2f, 3f, 4f), b / 2f);
        Assert.Equal(new Vector3(4f, 3f, 8f / 3f), b / a);
    }

    [Fact]
    public void Equality_ComparesComponents()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(1f, 2f, 3f);
        var c = new Vector3(1f, 2f, 4f);
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}