namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Vector3"/>: constants, engine directions,
/// metrics, cross product, operators and equality.
/// </summary>
public sealed class Vector3Tests
{
    [Test]
    public void Constructor_StoresComponents()
    {
        var v = new Vector3(1f, -2f, 3f);
        Assert.That(v.X, Is.EqualTo(1f));
        Assert.That(v.Y, Is.EqualTo(-2f));
        Assert.That(v.Z, Is.EqualTo(3f));
    }

    [Test]
    public void Vector2Constructor_AppendsZ()
    {
        Assert.That(new Vector3(new Vector2(1f, 2f), 3f), Is.EqualTo(new Vector3(1f, 2f, 3f)));
    }

    [Test]
    public void EngineDirections_FollowRightHandedYUpMinusZForward()
    {
        Assert.That(Vector3.Right, Is.EqualTo(Vector3.UnitX));
        Assert.That(Vector3.Up, Is.EqualTo(Vector3.UnitY));
        Assert.That(Vector3.Forward, Is.EqualTo(new Vector3(0f, 0f, -1f)));
    }

    [Test]
    public void Length_OfOneTwoTwo_IsThree()
    {
        Assert.That(new Vector3(1f, 2f, 2f).Length, Is.EqualTo(3f));
        Assert.That(new Vector3(1f, 2f, 2f).LengthSquared, Is.EqualTo(9f));
    }

    [Test]
    public void Normalized_HasLengthOne()
    {
        Assert.That(new Vector3(1f, 2f, 2f).Normalized.Length, Is.EqualTo(1f).Within(1e-5f));
    }

    [Test]
    public void Dot_PerpendicularVectors_IsZero()
    {
        Assert.That(Vector3.Dot(Vector3.UnitX, Vector3.UnitY), Is.EqualTo(0f));
    }

    [Test]
    public void Cross_FollowsRightHandRule()
    {
        Assert.That(Vector3.Cross(Vector3.UnitX, Vector3.UnitY), Is.EqualTo(Vector3.UnitZ));
        Assert.That(Vector3.Cross(Vector3.UnitY, Vector3.UnitZ), Is.EqualTo(Vector3.UnitX));
        Assert.That(Vector3.Cross(Vector3.UnitZ, Vector3.UnitX), Is.EqualTo(Vector3.UnitY));
    }

    [Test]
    public void Cross_IsAnticommutative()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(4f, 5f, 6f);
        Assert.That(-Vector3.Cross(b, a), Is.EqualTo(Vector3.Cross(a, b)));
    }

    [Test]
    public void Cross_IsPerpendicularToBothOperands()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(4f, 5f, 6f);
        var c = Vector3.Cross(a, b);
        Assert.That(Vector3.Dot(c, a), Is.EqualTo(0f).Within(1e-5f));
        Assert.That(Vector3.Dot(c, b), Is.EqualTo(0f).Within(1e-5f));
    }

    [Test]
    public void Lerp_AtBoundsAndMidpoint()
    {
        var a = new Vector3(0f, 0f, 0f);
        var b = new Vector3(10f, -2f, 4f);
        Assert.That(Vector3.Lerp(a, b, 0f), Is.EqualTo(a));
        Assert.That(Vector3.Lerp(a, b, 1f), Is.EqualTo(b));
        Assert.That(Vector3.Lerp(a, b, 0.5f), Is.EqualTo(new Vector3(5f, -1f, 2f)));
    }

    [Test]
    public void Operators_ComputeComponentWise()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(4f, 6f, 8f);
        Assert.That(a + b, Is.EqualTo(new Vector3(5f, 8f, 11f)));
        Assert.That(a - b, Is.EqualTo(new Vector3(-3f, -4f, -5f)));
        Assert.That(-a, Is.EqualTo(new Vector3(-1f, -2f, -3f)));
        Assert.That(a * b, Is.EqualTo(new Vector3(4f, 12f, 24f)));
        Assert.That(a * 2f, Is.EqualTo(new Vector3(2f, 4f, 6f)));
        Assert.That(2f * a, Is.EqualTo(new Vector3(2f, 4f, 6f)));
        Assert.That(b / 2f, Is.EqualTo(new Vector3(2f, 3f, 4f)));
        Assert.That(b / a, Is.EqualTo(new Vector3(4f, 3f, 8f / 3f)));
    }

    [Test]
    public void Equality_ComparesComponents()
    {
        var a = new Vector3(1f, 2f, 3f);
        var b = new Vector3(1f, 2f, 3f);
        var c = new Vector3(1f, 2f, 4f);
        Assert.That(a == b, Is.True);
        Assert.That(a != c, Is.True);
        Assert.That(b.GetHashCode(), Is.EqualTo(a.GetHashCode()));
    }
}
