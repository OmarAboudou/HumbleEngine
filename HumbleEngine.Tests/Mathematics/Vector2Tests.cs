namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Vector2"/>: constants, metrics, operators and equality.
/// </summary>
public sealed class Vector2Tests
{
    [Test]
    public void Constructor_StoresComponents()
    {
        var v = new Vector2(3f, -4f);
        Assert.That(v.X, Is.EqualTo(3f));
        Assert.That(v.Y, Is.EqualTo(-4f));
    }

    [Test]
    public void Constants_HaveExpectedComponents()
    {
        Assert.That(Vector2.Zero, Is.EqualTo(new Vector2(0f, 0f)));
        Assert.That(Vector2.One, Is.EqualTo(new Vector2(1f, 1f)));
        Assert.That(Vector2.UnitX, Is.EqualTo(new Vector2(1f, 0f)));
        Assert.That(Vector2.UnitY, Is.EqualTo(new Vector2(0f, 1f)));
    }

    [Test]
    public void Length_OfThreeFour_IsFive()
    {
        Assert.That(new Vector2(3f, 4f).Length, Is.EqualTo(5f));
    }

    [Test]
    public void LengthSquared_AvoidsSquareRoot()
    {
        Assert.That(new Vector2(3f, 4f).LengthSquared, Is.EqualTo(25f));
    }

    [Test]
    public void Normalized_HasLengthOne_AndKeepsDirection()
    {
        var n = new Vector2(3f, 4f).Normalized;
        Assert.That(n.Length, Is.EqualTo(1f).Within(1e-5f));
        Assert.That(n.X, Is.EqualTo(0.6f).Within(1e-5f));
        Assert.That(n.Y, Is.EqualTo(0.8f).Within(1e-5f));
    }

    [Test]
    public void Normalized_ZeroVector_YieldsNaN()
    {
        var n = Vector2.Zero.Normalized;
        Assert.That(float.IsNaN(n.X), Is.True);
        Assert.That(float.IsNaN(n.Y), Is.True);
    }

    [Test]
    public void Dot_PerpendicularVectors_IsZero()
    {
        Assert.That(Vector2.Dot(Vector2.UnitX, Vector2.UnitY), Is.EqualTo(0f));
    }

    [Test]
    public void Dot_OpposedVectors_IsNegative()
    {
        Assert.That(Vector2.Dot(Vector2.UnitX, -Vector2.UnitX), Is.LessThan(0f));
    }

    [Test]
    public void Distance_BetweenPoints_IsEuclidean()
    {
        Assert.That(Vector2.Distance(new Vector2(1f, 1f), new Vector2(4f, 5f)), Is.EqualTo(5f));
    }

    [Test]
    public void Lerp_AtBoundsAndMidpoint()
    {
        var a = new Vector2(0f, 0f);
        var b = new Vector2(10f, -2f);
        Assert.That(Vector2.Lerp(a, b, 0f), Is.EqualTo(a));
        Assert.That(Vector2.Lerp(a, b, 1f), Is.EqualTo(b));
        Assert.That(Vector2.Lerp(a, b, 0.5f), Is.EqualTo(new Vector2(5f, -1f)));
    }

    [Test]
    public void Operators_ComputeComponentWise()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(3f, 4f);
        Assert.That(a + b, Is.EqualTo(new Vector2(4f, 6f)));
        Assert.That(a - b, Is.EqualTo(new Vector2(-2f, -2f)));
        Assert.That(-a, Is.EqualTo(new Vector2(-1f, -2f)));
        Assert.That(a * b, Is.EqualTo(new Vector2(3f, 8f)));
        Assert.That(a * 2f, Is.EqualTo(new Vector2(2f, 4f)));
        Assert.That(2f * a, Is.EqualTo(new Vector2(2f, 4f)));
        Assert.That(a / 2f, Is.EqualTo(new Vector2(0.5f, 1f)));
        Assert.That(b / a, Is.EqualTo(new Vector2(3f, 2f)));
    }

    [Test]
    public void Equality_ComparesComponents()
    {
        var a = new Vector2(1f, 2f);
        var b = new Vector2(1f, 2f);
        var c = new Vector2(1f, 3f);
        Assert.That(a == b, Is.True);
        Assert.That(a != c, Is.True);
        Assert.That(b.GetHashCode(), Is.EqualTo(a.GetHashCode()));
    }
}
