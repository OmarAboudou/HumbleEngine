namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Rect"/>: corners, containment convention,
/// intersection, union and transformations.
/// </summary>
public sealed class RectTests
{
    [Test]
    public void Constructor_StoresComponents()
    {
        var r = new Rect(1f, 2f, 30f, 40f);
        Assert.That(r.X, Is.EqualTo(1f));
        Assert.That(r.Y, Is.EqualTo(2f));
        Assert.That(r.Width, Is.EqualTo(30f));
        Assert.That(r.Height, Is.EqualTo(40f));
    }

    [Test]
    public void VectorConstructor_MatchesScalarConstructor()
    {
        Assert.That(
            new Rect(new Vector2(1f, 2f), new Vector2(30f, 40f)),
            Is.EqualTo(new Rect(1f, 2f, 30f, 40f)));
    }

    [Test]
    public void DerivedPoints_AreComputedFromPositionAndSize()
    {
        var r = new Rect(10f, 20f, 30f, 40f);
        Assert.That(r.Position, Is.EqualTo(new Vector2(10f, 20f)));
        Assert.That(r.Size, Is.EqualTo(new Vector2(30f, 40f)));
        Assert.That(r.End, Is.EqualTo(new Vector2(40f, 60f)));
        Assert.That(r.Center, Is.EqualTo(new Vector2(25f, 40f)));
    }

    [Test]
    public void IsEmpty_DetectsZeroOrNegativeExtent()
    {
        Assert.That(Rect.Zero.IsEmpty, Is.True);
        Assert.That(new Rect(0f, 0f, 10f, 0f).IsEmpty, Is.True);
        Assert.That(new Rect(0f, 0f, -5f, 10f).IsEmpty, Is.True);
        Assert.That(new Rect(0f, 0f, 1f, 1f).IsEmpty, Is.False);
    }

    [Test]
    public void Contains_IsHalfOpen_MinEdgeInside_MaxEdgeOutside()
    {
        var r = new Rect(0f, 0f, 10f, 10f);
        Assert.That(r.Contains(new Vector2(0f, 0f)), Is.True);
        Assert.That(r.Contains(new Vector2(5f, 5f)), Is.True);
        Assert.That(r.Contains(new Vector2(10f, 5f)), Is.False);
        Assert.That(r.Contains(new Vector2(5f, 10f)), Is.False);
        Assert.That(r.Contains(new Vector2(-0.1f, 5f)), Is.False);
    }

    [Test]
    public void Contains_AdjacentRects_NeverBothContainSharedEdgePoint()
    {
        var left = new Rect(0f, 0f, 10f, 10f);
        var right = new Rect(10f, 0f, 10f, 10f);
        var onSharedEdge = new Vector2(10f, 5f);
        Assert.That(left.Contains(onSharedEdge), Is.False);
        Assert.That(right.Contains(onSharedEdge), Is.True);
    }

    [Test]
    public void Intersects_OverlappingRects_ReturnsTrue()
    {
        Assert.That(new Rect(0f, 0f, 10f, 10f).Intersects(new Rect(5f, 5f, 10f, 10f)), Is.True);
    }

    [Test]
    public void Intersects_AdjacentRects_ReturnsFalse()
    {
        Assert.That(new Rect(0f, 0f, 10f, 10f).Intersects(new Rect(10f, 0f, 10f, 10f)), Is.False);
    }

    [Test]
    public void Intersection_OverlappingRects_ReturnsOverlap()
    {
        var overlap = new Rect(0f, 0f, 10f, 10f).Intersection(new Rect(5f, 5f, 10f, 10f));
        Assert.That(overlap, Is.EqualTo(new Rect(5f, 5f, 5f, 5f)));
    }

    [Test]
    public void Intersection_DisjointRects_ReturnsZero()
    {
        var result = new Rect(0f, 0f, 10f, 10f).Intersection(new Rect(20f, 20f, 5f, 5f));
        Assert.That(result, Is.EqualTo(Rect.Zero));
    }

    [Test]
    public void Union_ContainsBothRects()
    {
        var union = new Rect(0f, 0f, 10f, 10f).Union(new Rect(20f, 5f, 10f, 10f));
        Assert.That(union, Is.EqualTo(new Rect(0f, 0f, 30f, 15f)));
    }

    [Test]
    public void Translated_MovesPositionKeepsSize()
    {
        var r = new Rect(1f, 2f, 3f, 4f).Translated(new Vector2(10f, 20f));
        Assert.That(r, Is.EqualTo(new Rect(11f, 22f, 3f, 4f)));
    }

    [Test]
    public void Grown_ExpandsAllSides_NegativeShrinks()
    {
        var r = new Rect(10f, 10f, 20f, 20f);
        Assert.That(r.Grown(5f), Is.EqualTo(new Rect(5f, 5f, 30f, 30f)));
        Assert.That(r.Grown(-5f), Is.EqualTo(new Rect(15f, 15f, 10f, 10f)));
    }

    [Test]
    public void Equality_ComparesAllComponents()
    {
        var a = new Rect(1f, 2f, 3f, 4f);
        var b = new Rect(1f, 2f, 3f, 4f);
        var c = new Rect(1f, 2f, 3f, 5f);
        Assert.That(a == b, Is.True);
        Assert.That(a != c, Is.True);
        Assert.That(b.GetHashCode(), Is.EqualTo(a.GetHashCode()));
    }
}
