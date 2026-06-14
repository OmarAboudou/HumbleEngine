namespace HumbleEngine.Tests.SceneGraph;

/// <summary>
/// Unit tests for <see cref="Constraints"/>: the tight/loose/unbounded factories,
/// clamping via <see cref="Constraints.Constrain"/>, <see cref="Constraints.Loosen"/>,
/// and the structural equality the layout relies on for skip-unchanged.
/// </summary>
public sealed class ConstraintsTests
{
    [Test]
    public void Tight_PinsBothAxes()
    {
        var c = Constraints.Tight(new Vector2(120f, 40f));

        Assert.That(c.IsTight, Is.True);
        Assert.That(c.Min, Is.EqualTo(new Vector2(120f, 40f)));
        Assert.That(c.Max, Is.EqualTo(new Vector2(120f, 40f)));
    }

    [Test]
    public void Tight_Constrain_AlwaysReturnsThePinnedSize()
    {
        var c = Constraints.Tight(120f, 40f);

        // Whatever the child asks for, a tight constraint hands back its exact size.
        Assert.That(c.Constrain(new Vector2(999f, 1f)), Is.EqualTo(new Vector2(120f, 40f)));
        Assert.That(c.Constrain(Vector2.Zero), Is.EqualTo(new Vector2(120f, 40f)));
    }

    [Test]
    public void Loose_AllowsUpToTheMax_FromZero()
    {
        var c = Constraints.Loose(new Vector2(200f, 100f));

        Assert.That(c.IsTight, Is.False);
        Assert.That(c.Min, Is.EqualTo(Vector2.Zero));
        Assert.That(c.Max, Is.EqualTo(new Vector2(200f, 100f)));
    }

    [Test]
    public void Constrain_ClampsBelowMinAndAboveMax()
    {
        var c = new Constraints(50f, 200f, 30f, 100f);

        Assert.That(c.Constrain(new Vector2(10f, 10f)),  Is.EqualTo(new Vector2(50f, 30f)));   // below min
        Assert.That(c.Constrain(new Vector2(999f, 999f)), Is.EqualTo(new Vector2(200f, 100f))); // above max
        Assert.That(c.Constrain(new Vector2(120f, 60f)),  Is.EqualTo(new Vector2(120f, 60f)));  // within
    }

    [Test]
    public void Unbounded_LeavesAnyNonNegativeSizeUntouched()
    {
        var c = Constraints.Unbounded;

        Assert.That(c.Constrain(new Vector2(1234f, 5678f)), Is.EqualTo(new Vector2(1234f, 5678f)));
        Assert.That(float.IsPositiveInfinity(c.MaxWidth), Is.True);
        Assert.That(float.IsPositiveInfinity(c.MaxHeight), Is.True);
    }

    [Test]
    public void Loosen_DropsMinsKeepsMaxes()
    {
        var c = Constraints.Tight(120f, 40f).Loosen();

        Assert.That(c.Min, Is.EqualTo(Vector2.Zero));
        Assert.That(c.Max, Is.EqualTo(new Vector2(120f, 40f)));
        Assert.That(c.IsTight, Is.False);
    }

    [Test]
    public void Equality_IsStructural_SoChangeDetectionWorks()
    {
        var a = new Constraints(0f, 100f, 0f, 50f);
        var b = new Constraints(0f, 100f, 0f, 50f);
        var d = new Constraints(0f, 100f, 0f, 60f);

        Assert.That(a, Is.EqualTo(b));
        Assert.That(a == b, Is.True);
        Assert.That(a, Is.Not.EqualTo(d));
        Assert.That(a != d, Is.True);
    }
}
