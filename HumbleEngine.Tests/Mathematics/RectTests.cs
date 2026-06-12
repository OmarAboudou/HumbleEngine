namespace HumbleEngine.Tests.Mathematics;

/// <summary>
/// Unit tests for <see cref="Rect"/>: corners, containment convention,
/// intersection, union and transformations.
/// </summary>
public sealed class RectTests
{
    [Fact]
    public void Constructor_StoresComponents()
    {
        var r = new Rect(1f, 2f, 30f, 40f);
        Assert.Equal(1f, r.X);
        Assert.Equal(2f, r.Y);
        Assert.Equal(30f, r.Width);
        Assert.Equal(40f, r.Height);
    }

    [Fact]
    public void VectorConstructor_MatchesScalarConstructor()
    {
        Assert.Equal(
            new Rect(1f, 2f, 30f, 40f),
            new Rect(new Vector2(1f, 2f), new Vector2(30f, 40f)));
    }

    [Fact]
    public void DerivedPoints_AreComputedFromPositionAndSize()
    {
        var r = new Rect(10f, 20f, 30f, 40f);
        Assert.Equal(new Vector2(10f, 20f), r.Position);
        Assert.Equal(new Vector2(30f, 40f), r.Size);
        Assert.Equal(new Vector2(40f, 60f), r.End);
        Assert.Equal(new Vector2(25f, 40f), r.Center);
    }

    [Fact]
    public void IsEmpty_DetectsZeroOrNegativeExtent()
    {
        Assert.True(Rect.Zero.IsEmpty);
        Assert.True(new Rect(0f, 0f, 10f, 0f).IsEmpty);
        Assert.True(new Rect(0f, 0f, -5f, 10f).IsEmpty);
        Assert.False(new Rect(0f, 0f, 1f, 1f).IsEmpty);
    }

    [Fact]
    public void Contains_IsHalfOpen_MinEdgeInside_MaxEdgeOutside()
    {
        var r = new Rect(0f, 0f, 10f, 10f);
        Assert.True(r.Contains(new Vector2(0f, 0f)));
        Assert.True(r.Contains(new Vector2(5f, 5f)));
        Assert.False(r.Contains(new Vector2(10f, 5f)));
        Assert.False(r.Contains(new Vector2(5f, 10f)));
        Assert.False(r.Contains(new Vector2(-0.1f, 5f)));
    }

    [Fact]
    public void Contains_AdjacentRects_NeverBothContainSharedEdgePoint()
    {
        var left = new Rect(0f, 0f, 10f, 10f);
        var right = new Rect(10f, 0f, 10f, 10f);
        var onSharedEdge = new Vector2(10f, 5f);
        Assert.False(left.Contains(onSharedEdge));
        Assert.True(right.Contains(onSharedEdge));
    }

    [Fact]
    public void Intersects_OverlappingRects_ReturnsTrue()
    {
        Assert.True(new Rect(0f, 0f, 10f, 10f).Intersects(new Rect(5f, 5f, 10f, 10f)));
    }

    [Fact]
    public void Intersects_AdjacentRects_ReturnsFalse()
    {
        Assert.False(new Rect(0f, 0f, 10f, 10f).Intersects(new Rect(10f, 0f, 10f, 10f)));
    }

    [Fact]
    public void Intersection_OverlappingRects_ReturnsOverlap()
    {
        var overlap = new Rect(0f, 0f, 10f, 10f).Intersection(new Rect(5f, 5f, 10f, 10f));
        Assert.Equal(new Rect(5f, 5f, 5f, 5f), overlap);
    }

    [Fact]
    public void Intersection_DisjointRects_ReturnsZero()
    {
        var result = new Rect(0f, 0f, 10f, 10f).Intersection(new Rect(20f, 20f, 5f, 5f));
        Assert.Equal(Rect.Zero, result);
    }

    [Fact]
    public void Union_ContainsBothRects()
    {
        var union = new Rect(0f, 0f, 10f, 10f).Union(new Rect(20f, 5f, 10f, 10f));
        Assert.Equal(new Rect(0f, 0f, 30f, 15f), union);
    }

    [Fact]
    public void Translated_MovesPositionKeepsSize()
    {
        var r = new Rect(1f, 2f, 3f, 4f).Translated(new Vector2(10f, 20f));
        Assert.Equal(new Rect(11f, 22f, 3f, 4f), r);
    }

    [Fact]
    public void Grown_ExpandsAllSides_NegativeShrinks()
    {
        var r = new Rect(10f, 10f, 20f, 20f);
        Assert.Equal(new Rect(5f, 5f, 30f, 30f), r.Grown(5f));
        Assert.Equal(new Rect(15f, 15f, 10f, 10f), r.Grown(-5f));
    }

    [Fact]
    public void Equality_ComparesAllComponents()
    {
        var a = new Rect(1f, 2f, 3f, 4f);
        var b = new Rect(1f, 2f, 3f, 4f);
        var c = new Rect(1f, 2f, 3f, 5f);
        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}